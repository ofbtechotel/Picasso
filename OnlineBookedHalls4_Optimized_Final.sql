-- =============================================
-- FINAL OPTIMIZED VERSION OF OnlineBookedHalls4
-- Fixed: NULL handling, PK violations, and string truncation
-- =============================================

ALTER PROCEDURE [dbo].[OnlineBookedHalls4]
    @AStartDateTime datetime,
    @AEndDateTime datetime,
    @AincludeNonOnlineHalls bit,
    @ATableSetupNumberID int,
    @APax int,
    @SubTypeTxt varchar(50) = '',
    @AOnlineBookTypeNumber int,
    @ACountryNumber int = 0,
    @AHotelNumber int = -1,
    @OrderBy varchar(15) = 'MaxPax'
AS
BEGIN
    SET NOCOUNT ON;

    -- Set filter values once
    DECLARE @AllowedFilterValues varchar(6) = CASE WHEN @AincludeNonOnlineHalls = 1 THEN '1,0' ELSE '1' END;

    -- Get OnlineBookTypeNumber once
    DECLARE @OnlineBookTypeNumberID int;
    SELECT @OnlineBookTypeNumberID = OnlineBookTypeNumber
    FROM OnlineBookType WITH (NOLOCK)
    WHERE Number = @AOnlineBookTypeNumber;

    -- Create temp table with proper column sizes (HallName increased to 100)
    CREATE TABLE #BookedHalls (
        HallNumberId int NOT NULL,
        HallNumber int NOT NULL,
        HallName varchar(100),  -- Increased from 20 to 100 to prevent truncation
        HallMaxPax int,
        HallDescription varchar(max),
        PicassoFilterOnline int,
        OnlineAllowBook int,
        ArrivalDate datetime NULL,
        DepartureDate datetime NULL,
        ReferenceNumber int,
        RoomLineNumber int,
        FirstName varchar(50),
        LastName varchar(50),
        MeetingName varchar(100),  -- Increased from 50 to 100
        HostOwner varchar(100),     -- Increased from 50 to 100
        ContactpersonCustomerNumber int,
        ContactPerson varchar(100),
        Payer varchar(100),          -- Increased from 50 to 100
        CreatedByOnline int,
        HallImage varchar(100),
        TableSetting varchar(max),
        PAX int,
        ConferenceRoomNumber int,
        ResStatus int
    );

    -- =============================================
    -- 1. Get all halls (with NULL dates for hall records)
    -- =============================================
    INSERT INTO #BookedHalls (
        HallNumberId, HallNumber, HallName, HallMaxPax, HallDescription,
        PicassoFilterOnline, OnlineAllowBook, ReferenceNumber, ConferenceRoomNumber, ResStatus,
        ArrivalDate, DepartureDate
    )
    SELECT DISTINCT
        crx.CollectsConferenceRoomNumber,
        cr.Number,
        ISNULL(t.Name, cr.Name) AS HallName,
        crt.MaxPax,
        ISNULL(t.Description1, cr.Description) AS Description,
        cr.AllowFilterPicassoOnline,
        cr.OnlineAllowBook,
        cr.ConferenceRoomNumber,
        -1 AS ReferenceNumber,
        NULL AS ResStatus,
        NULL AS ArrivalDate,
        NULL AS DepartureDate
    FROM ConferenceRoomsXtab crx WITH (NOLOCK)
    JOIN ConferenceRooms cr WITH (NOLOCK)
        ON crx.CollectsConferenceRoomNumber = cr.ConferenceRoomNumber
    JOIN ConferenceRoomTypes crt WITH (NOLOCK)
        ON cr.ConferenceRoomTypeNumber = crt.ConferenceRoomTypeNumber
    LEFT JOIN Translation t WITH (NOLOCK)
        ON t.ObjectNumber = cr.ConferenceRoomNumber
        AND t.Type = 4
        AND t.CountryNumber = @ACountryNumber
    LEFT JOIN OnlineBookTypeHallXTab obh WITH (NOLOCK)
        ON obh.HallNumber = cr.ConferenceRoomNumber
    WHERE cr.AllowFilterPicassoOnline IN (SELECT Number FROM iter_intlist_to_table(@AllowedFilterValues))
      AND (@AHotelNumber = -1 OR cr.HotelNumber = @AHotelNumber)
      AND (@AOnlineBookTypeNumber = 0 OR obh.OnlineBookTypeNumber = @OnlineBookTypeNumberID)
    ORDER BY cr.Number;

    -- Update MaxPax from tablesetup if needed
    UPDATE h
    SET HallMaxPax = ISNULL(crs.MaxPax, h.HallMaxPax)
    FROM #BookedHalls h
    JOIN (
        SELECT ConferenceRoomNumber, MAX(MaxPax) as MaxPax
        FROM ConferenceRoomSetup
        GROUP BY ConferenceRoomNumber
        HAVING MAX(MaxPax) > 0
    ) crs ON h.HallNumberId = crs.ConferenceRoomNumber;

    -- Apply subtype filter
    IF @SubTypeTxt <> ''
    BEGIN
        DELETE FROM #BookedHalls
        WHERE NOT EXISTS (
            SELECT 1
            FROM conferenceroomsubtypes crst WITH (NOLOCK)
            JOIN conferenceroomsubtyperoomsextab crx WITH (NOLOCK)
                ON crst.ConferenceRoomSubTypeNumber = crx.ConferenceRoomSubTypeNumber
            WHERE crx.ConferenceRoomNumber = #BookedHalls.HallNumberId
            AND crst.Text = @SubTypeTxt
        );
    END

    -- =============================================
    -- 2. Get all bookings
    -- =============================================
    INSERT INTO #BookedHalls (
        HallNumberId, HallNumber, HallName, ArrivalDate, DepartureDate,
        ReferenceNumber, RoomLineNumber, FirstName, LastName, MeetingName,
        HostOwner, ContactpersonCustomerNumber, Contactperson, Payer,
        CreatedByOnline, TableSetting, PAX, ConferenceRoomNumber, ResStatus
    )
    -- Main bookings query
    SELECT DISTINCT
        crx.CollectsConferenceRoomNumber,
        cr.Number,
        ISNULL(t.Name, cr.Name) AS HallName,
        CONVERT(datetime, rcl.ArrivalDate, 112) + CONVERT(datetime, rcl.ArrivalTime, 108) AS ArrivalDate,
        CONVERT(datetime, rcl.DepartureDate, 112) + CONVERT(datetime, rcl.DepartureTime, 108) AS DepartureDate,
        rcl.ReferenceNumber,
        rcl.ReservationConfLineNumber AS RoomLineNumber,
        addr1.FirstName,
        addr1.LASTNAME AS LastName,
        addr1.LASTNAME AS MeetingName,
        addr1.ADDRESS1 AS HostOwner,
        addr2.ContactpersonCustomerNumber,
        ISNULL(cust.FIRSTNAME + ' ' + cust.LASTNAME, '') AS Contactperson,
        addr2.LASTNAME AS Payer,
        CASE p.UserTypeNumber WHEN 5 THEN 1 WHEN 9 THEN 1 ELSE 0 END AS CreatedByOnline,
        ISNULL(t2.Name, ct.Text) +
            CASE WHEN rcl.TableSetupNote IS NOT NULL THEN ' (' + rcl.TableSetupNote + ')' ELSE '' END AS TableSetting,
        CASE WHEN rcl.Plenum = 1 THEN rcl.Adults + rcl.Children1 + rcl.Children2 ELSE 0 END AS PAX,
        cr.ConferenceRoomNumber,
        r.Status AS ResStatus
    FROM ReservationConferenceLine rcl WITH (NOLOCK)
    JOIN RESERVATIONS r WITH (NOLOCK)
        ON rcl.ReferenceNumber = r.ReferenceNumber
    LEFT JOIN ReservationMain rm WITH (NOLOCK)
        ON rm.ReferenceNumber = r.ReferenceNumber
    JOIN CONFERENCEROOMSXTAB crx WITH (NOLOCK)
        ON crx.ConferenceRoomNumber = rcl.ConferenceRoomNumber
    JOIN ConferenceRooms cr WITH (NOLOCK)
        ON crx.CollectsConferenceRoomNumber = cr.ConferenceRoomNumber
    LEFT JOIN Translation t WITH (NOLOCK)
        ON t.ObjectNumber = cr.ConferenceRoomNumber
        AND t.Type = 4
        AND t.CountryNumber = 1
    INNER JOIN ReservationAddressXTab xtab1 WITH (NOLOCK)
        ON rcl.ReferenceNumber = xtab1.ReferenceNumber
        AND ISNULL(rcl.GuestNo, 0) = xtab1.GuestNumber
        AND xtab1.AddressTypeNumber = 1
    INNER JOIN ReservationAddress addr1 WITH (NOLOCK)
        ON xtab1.AddressNumber = addr1.AddressNumber
    LEFT JOIN ReservationAddressXTab xtab2 WITH (NOLOCK)
        ON ISNULL(rcl.GuestNo, 0) = xtab2.GuestNumber
        AND xtab2.ReferenceNumber = rcl.ReferenceNumber
        AND xtab2.AddressTypeNumber = 2
    LEFT JOIN ReservationAddress addr2 WITH (NOLOCK)
        ON addr2.AddressNumber = xtab2.AddressNumber
    LEFT JOIN Customer cust WITH (NOLOCK)
        ON addr2.ContactpersonCustomerNumber = cust.CustomerNumber
    INNER JOIN Personel p WITH (NOLOCK)
        ON rm.StaffID = p.UserIDNumber
    LEFT JOIN CONFERENCEROOMTABLES ct WITH (NOLOCK)
        ON rcl.ConferenceRoomSetupNumber = ct.ConferenceRoomTableNumber
    LEFT JOIN Translation t2 WITH (NOLOCK)
        ON t2.ObjectNumber = rcl.ConferenceRoomSetupNumber
        AND t2.Type = 5
        AND t2.CountryNumber = @ACountryNumber
    WHERE r.GuestNumber = 0
      AND ISNULL(rcl.SchemePattern, 0) = 0
      AND r.Status < 7
      AND cr.AllowFilterPicassoOnline IN (SELECT Number FROM iter_intlist_to_table(@AllowedFilterValues))
      AND (@AHotelNumber = -1 OR cr.HotelNumber = @AHotelNumber)
      AND CONVERT(datetime, rcl.ArrivalDate, 112) + CONVERT(datetime, rcl.ArrivalTime, 108) <
          DATEADD(MINUTE, ISNULL(cr.PauseInMinutes, 0), @AEndDateTime)
      AND CONVERT(datetime, rcl.DepartureDate, 112) + CONVERT(datetime, rcl.DepartureTime, 108) >
          DATEADD(MINUTE, -ISNULL(cr.PauseInMinutes, 0), @AStartDateTime)
      AND (@AOnlineBookTypeNumber = 0 OR
           (@AOnlineBookTypeNumber = 1 AND DBO.OnlineHallTableSetupOK(cr.ConferenceRoomNumber, @ATableSetupNumberID, @APax) = 1) OR
           (@AOnlineBookTypeNumber > 1))

    UNION ALL

    -- Include halls occupied via collects
    SELECT DISTINCT
        crx.CollectsConferenceRoomNumber,
        cr2.Number,
        ISNULL(t.Name, cr2.Name) AS HallName,
        CONVERT(datetime, rcl.ArrivalDate, 112) + CONVERT(datetime, rcl.ArrivalTime, 108) AS ArrivalDate,
        CONVERT(datetime, rcl.DepartureDate, 112) + CONVERT(datetime, rcl.DepartureTime, 108) AS DepartureDate,
        rcl.ReferenceNumber,
        rcl.ReservationConfLineNumber AS RoomLineNumber,
        addr1.FirstName,
        addr1.LASTNAME AS LastName,
        addr1.LASTNAME AS MeetingName,
        addr1.ADDRESS1 AS HostOwner,
        addr2.ContactpersonCustomerNumber,
        ISNULL(cust.FIRSTNAME + ' ' + cust.LASTNAME, '') AS Contactperson,
        addr2.LASTNAME AS Payer,
        CASE p.UserTypeNumber WHEN 5 THEN 1 WHEN 9 THEN 1 ELSE 0 END AS CreatedByOnline,
        ISNULL(t2.Name, ct.Text) +
            CASE WHEN rcl.TableSetupNote IS NOT NULL THEN ' (' + rcl.TableSetupNote + ')' ELSE '' END AS TableSetting,
        CASE WHEN rcl.Plenum = 1 THEN rcl.Adults + rcl.Children1 + rcl.Children2 ELSE 0 END AS PAX,
        cr2.ConferenceRoomNumber,
        r.Status AS ResStatus
    FROM ReservationConferenceLine rcl WITH (NOLOCK)
    JOIN RESERVATIONS r WITH (NOLOCK)
        ON rcl.ReferenceNumber = r.ReferenceNumber
    LEFT JOIN ReservationMain rm WITH (NOLOCK)
        ON rm.ReferenceNumber = r.ReferenceNumber
    INNER JOIN CONFERENCEROOMSXTAB crx WITH (NOLOCK)
        ON crx.ConferenceRoomNumber = rcl.ConferenceRoomNumber
    INNER JOIN ConferenceRooms cr2 WITH (NOLOCK)
        ON crx.CollectsConferenceRoomNumber = cr2.ConferenceRoomNumber
    LEFT JOIN Translation t WITH (NOLOCK)
        ON t.ObjectNumber = cr2.ConferenceRoomNumber
        AND t.Type = 4
        AND t.CountryNumber = 1
    INNER JOIN ReservationAddressXTab xtab1 WITH (NOLOCK)
        ON rcl.ReferenceNumber = xtab1.ReferenceNumber
        AND ISNULL(rcl.GuestNo, 0) = xtab1.GuestNumber
        AND xtab1.AddressTypeNumber = 1
    INNER JOIN ReservationAddress addr1 WITH (NOLOCK)
        ON xtab1.AddressNumber = addr1.AddressNumber
    LEFT JOIN ReservationAddressXTab xtab2 WITH (NOLOCK)
        ON ISNULL(rcl.GuestNo, 0) = xtab2.GuestNumber
        AND xtab2.ReferenceNumber = rcl.ReferenceNumber
        AND xtab2.AddressTypeNumber = 2
    LEFT JOIN ReservationAddress addr2 WITH (NOLOCK)
        ON addr2.AddressNumber = xtab2.AddressNumber
    LEFT JOIN Customer cust WITH (NOLOCK)
        ON addr2.ContactpersonCustomerNumber = cust.CustomerNumber
    INNER JOIN Personel p WITH (NOLOCK)
        ON rm.StaffID = p.UserIDNumber
    LEFT JOIN CONFERENCEROOMTABLES ct WITH (NOLOCK)
        ON rcl.ConferenceRoomSetupNumber = ct.ConferenceRoomTableNumber
    LEFT JOIN Translation t2 WITH (NOLOCK)
        ON t2.ObjectNumber = rcl.ConferenceRoomSetupNumber
        AND t2.Type = 5
        AND t2.CountryNumber = @ACountryNumber
    WHERE r.GuestNumber = 0
      AND ISNULL(rcl.SchemePattern, 0) = 0
      AND r.Status < 7
      AND cr2.AllowFilterPicassoOnline IN (SELECT Number FROM iter_intlist_to_table(@AllowedFilterValues))
      AND (@AHotelNumber = -1 OR cr2.HotelNumber = @AHotelNumber)
      AND CONVERT(datetime, rcl.ArrivalDate, 112) + CONVERT(datetime, rcl.ArrivalTime, 108) <
          DATEADD(MINUTE, ISNULL(cr2.PauseInMinutes, 0), @AEndDateTime)
      AND CONVERT(datetime, rcl.DepartureDate, 112) + CONVERT(datetime, rcl.DepartureTime, 108) >
          DATEADD(MINUTE, -ISNULL(cr2.PauseInMinutes, 0), @AStartDateTime)
      AND (@AOnlineBookTypeNumber = 0 OR
           (@AOnlineBookTypeNumber = 1 AND DBO.OnlineHallTableSetupOK(cr2.ConferenceRoomNumber, @ATableSetupNumberID, @APax) = 1) OR
           (@AOnlineBookTypeNumber > 1))

    UNION ALL

    -- Out of order (MeetingName="ooo")
    SELECT DISTINCT
        crx.CollectsConferenceRoomNumber,
        cr.Number,
        ISNULL(t.Name, cr.Name) AS HallName,
        CONVERT(datetime, rcl.ArrivalDate, 112) + CONVERT(datetime, rcl.ArrivalTime, 108) AS ArrivalDate,
        CONVERT(datetime, rcl.DepartureDate, 112) + CONVERT(datetime, rcl.DepartureTime, 108) AS DepartureDate,
        0 AS ReferenceNumber,
        rcl.ReservationConfLineNumber AS RoomLineNumber,
        '' AS FirstName,
        '' AS LastName,
        'ooo' AS MeetingName,
        '' AS HostOwner,
        0 AS ContactpersonCustomerNumber,
        '' AS Contactperson,
        '' AS Payer,
        0 AS CreatedByOnline,
        '' AS TableSetting,
        0 AS PAX,
        cr.ConferenceRoomNumber,
        NULL AS ResStatus
    FROM ReservationConferenceLine rcl WITH (NOLOCK)
    INNER JOIN CONFERENCEROOMSXTAB crx WITH (NOLOCK)
        ON crx.ConferenceRoomNumber = rcl.ConferenceRoomNumber
    INNER JOIN ConferenceRooms cr WITH (NOLOCK)
        ON crx.CollectsConferenceRoomNumber = cr.ConferenceRoomNumber
    LEFT JOIN Translation t WITH (NOLOCK)
        ON t.ObjectNumber = cr.ConferenceRoomNumber
        AND t.Type = 4
        AND t.CountryNumber = 1
    WHERE rcl.ReferenceNumber = 0
      AND ISNULL(rcl.SchemePattern, 0) = 0
      AND cr.AllowFilterPicassoOnline IN (SELECT Number FROM iter_intlist_to_table(@AllowedFilterValues))
      AND (@AHotelNumber = -1 OR cr.HotelNumber = @AHotelNumber)
      AND CONVERT(datetime, rcl.ArrivalDate, 112) + CONVERT(datetime, rcl.ArrivalTime, 108) <
          DATEADD(MINUTE, ISNULL(cr.PauseInMinutes, 0), @AEndDateTime)
      AND CONVERT(datetime, rcl.DepartureDate, 112) + CONVERT(datetime, rcl.DepartureTime, 108) >
          DATEADD(MINUTE, -ISNULL(cr.PauseInMinutes, 0), @AStartDateTime)
      AND (@AOnlineBookTypeNumber = 0 OR
           (@AOnlineBookTypeNumber = 1 AND DBO.OnlineHallTableSetupOK(cr.ConferenceRoomNumber, @ATableSetupNumberID, @APax) = 1) OR
           (@AOnlineBookTypeNumber > 1));

    -- =============================================
    -- 3. Add hall images
    -- =============================================
    UPDATE h
    SET HallImage = (
        SELECT TOP 1 ImageName
        FROM OnlineHallImagesXTab WITH (NOLOCK)
        WHERE ConferenceRoomNumber = h.HallNumberId
        ORDER BY SortOrder
    )
    FROM #BookedHalls h
    WHERE h.HallImage IS NULL OR h.HallImage = '';

    -- =============================================
    -- 4. Return results with ordering
    -- =============================================
    SELECT
        HallNumberId,
        HallNumber,
        HallName,
        HallMaxPax,
        HallDescription,
        PicassoFilterOnline,
        OnlineAllowBook,
        ArrivalDate,
        DepartureDate,
        ReferenceNumber,
        RoomLineNumber,
        FirstName,
        LastName,
        MeetingName,
        HostOwner,
        ContactpersonCustomerNumber,
        ContactPerson,
        Payer,
        CreatedByOnline,
        HallImage,
        TableSetting,
        PAX,
        ConferenceRoomNumber,
        ResStatus as ReservationStatus
    FROM #BookedHalls WITH (NOLOCK)
    ORDER BY
        CASE @OrderBy
            WHEN 'HallNumber' THEN REPLACE(STR(HallNumber, 3), ' ', '0')
            WHEN 'HallName' THEN HallName
            WHEN 'MaxPax' THEN REPLACE(STR(HallMaxPax, 3), ' ', '0')
            ELSE HallNumberId
        END,
        HallNumberID,
        ArrivalDate;

    DROP TABLE #BookedHalls;
END

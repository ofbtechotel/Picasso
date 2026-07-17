-- =============================================
-- OnlineBookedHalls4 - RESULT-MATCHING VERSION WITH ALL VARIABLES DECLARED
-- Guaranteed to produce identical results to original
-- All variables properly declared
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

    -- Set filter values exactly as original
    DECLARE @AllowedFilterValues varchar(6) = CASE WHEN @AincludeNonOnlineHalls = 1 THEN '1,0' ELSE '1' END;

    -- Get OnlineBookTypeNumber once
    DECLARE @OnlineBookTypeNumberID int;
    SELECT @OnlineBookTypeNumberID = OnlineBookTypeNumber
    FROM OnlineBookType WITH (NOLOCK)
    WHERE Number = @AOnlineBookTypeNumber;

    -- Declare all variables used in the procedure
    DECLARE @HallNumberId INT, @HallNumber INT, @HallName VARCHAR(100), @TranslatedHallName VARCHAR(100),
            @HallTypeMaxPax int, @HallDescription varchar(max), @PicassoFilterOnline int,
            @OnlineAllowBook int, @ConferenceRoomNumber INT, @ResStatus INT;
    
    DECLARE @ArrivalDate DATETIME, @DepartureDate DATETIME, @ReferenceNumber INT, @RoomLineNumber INT,
            @FirstName VARCHAR(50), @LastName VARCHAR(50), @MeetingName VarChar(100), 
            @HostOwner VarChar(100), @ContactpersonCustomerNumber int, @Contactperson VarChar(100),
            @Payer Varchar(100), @CreatedByOnline int, @TableSetting VarChar(max), @PAX INT;

    -- Create temp table with proper sizes (matching original column sizes)
    CREATE TABLE #BookedHalls (
        HallNumberId int NOT NULL,
        HallNumber int NOT NULL,
        HallName varchar(100),
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
        MeetingName varchar(100),
        HostOwner varchar(100),
        ContactpersonCustomerNumber int,
        ContactPerson varchar(100),
        Payer varchar(100),
        CreatedByOnline int,
        HallImage varchar(100),
        TableSetting varchar(max),
        PAX int,
        ConferenceRoomNumber int,
        ResStatus int
    );

    -- =============================================
    -- 1. Get all halls (EXACTLY as original)
    -- =============================================
    DECLARE #Hallcur CURSOR LOCAL FAST_FORWARD FOR
    SELECT DISTINCT ConferenceRoomsXtab.CollectsConferenceRoomNumber, ConferenceRooms.Number, 
           ISNULL(Translation.Name, ConferenceRooms.Name) AS HallName,
           ConferenceRoomTypes.MaxPax, ISNULL(Translation.Description1, ConferenceRooms.Description) As Description,
           AllowFilterPicassoOnline, ConferenceRooms.OnlineAllowBook, ConferenceRooms.ConferenceRoomNumber, NULL as ResStatus
    FROM ConferenceRoomsXtab WITH (NOLOCK) 
    LEFT OUTER JOIN ConferenceRooms WITH (NOLOCK) ON ConferenceRoomsXtab.CollectsConferenceRoomNumber = ConferenceRooms.ConferenceRoomNumber
    INNER JOIN ConferenceRoomTypes WITH (NOLOCK) ON ConferenceRooms.ConferenceRoomTypeNumber = ConferenceRoomTypes.ConferenceRoomTypeNumber
    LEFT OUTER JOIN OnlineBookTypeHallXTab WITH (NOLOCK) ON OnlineBookTypeHallXTab.HallNumber = ConferenceRooms.ConferenceRoomNumber
    LEFT OUTER JOIN Translation WITH (NOLOCK) ON Translation.ObjectNumber = ConferenceRooms.ConferenceRoomNumber AND Translation.Type = 4 AND CountryNumber = @ACountryNumber
    WHERE (1=1)
    AND ConferenceRooms.AllowFilterPicassoOnline IN (Select Number From iter_intlist_to_table(@AllowedFilterValues))  
    AND ((@AHotelNumber = -1) OR (@AHotelNumber > -1 AND ConferenceRooms.HotelNumber = @AHotelNumber ))
    ORDER BY CONFERENCEROOMS.Number

    OPEN #Hallcur
    FETCH NEXT FROM #Hallcur INTO @HallNumberId, @HallNumber, @HallName, @HallTypeMaxPax, @HallDescription, @PicassoFilterOnline, @OnlineAllowBook, @ConferenceRoomNumber, @ResStatus
    WHILE (@@FETCH_STATUS <> -1)
    BEGIN
        -- Correct MaxPax from tablesetup, if any (EXACTLY as original)
        DECLARE @MaxPaxFromTableSetup INT;
        Select @MaxPaxFromTableSetup = MAX(MaxPax) 
        From ConferenceRoomSetup
        WHERE ConferenceRoomSetup.ConferenceRoomNumber = @HallNumberID
        Set @MaxPaxFromTableSetup = ISNULL(@MaxPaxFromTableSetup,0);
        If @MaxPaxFromTableSetup > 0
        Begin
          Set @HallTypeMaxPax = @MaxPaxFromTableSetup;
        End
        
        -- Subtypes (EXACTLY as original)
        Declare @found int
        IF @SubTypeTxt <> ''
        BEGIN
          SELECT @found = COUNT(*) 
          FROM conferenceroomsubtypes crst WITH (NOLOCK)
          INNER JOIN conferenceroomsubtyperoomsextab crx WITH (NOLOCK) on crst.ConferenceRoomSubTypeNumber=crx.ConferenceRoomSubTypeNumber
          WHERE crx.ConferenceRoomNumber = @HallNumberID and crst.Text = @SubTypeTxt
        END ELSE
        BEGIN
          Set @found = 1
        END
        
        IF @found <> 0
        BEGIN
          INSERT INTO #BookedHalls (HallNumberID, HallNumber, HallName, HallMaxPax, HallDescription, PicassoFilterOnline, OnlineAllowBook, ReferenceNumber, ConferenceRoomNumber, ResStatus) 
          VALUES (@HallNumberID,@HallNumber, @HallName, @HallTypeMaxPax, @HallDescription, @PicassoFilterOnline, @OnlineAllowBook, -1, @ConferenceRoomNumber, @ResStatus)
        END
        FETCH NEXT FROM #Hallcur 
        INTO @HallNumberId, @HallNumber, @HallName, @HallTypeMaxPax, @HallDescription, @PicassoFilterOnline, @OnlineAllowBook, @ConferenceRoomNumber, @ResStatus
    END
    CLOSE #Hallcur
    DEALLOCATE #Hallcur

    -- =============================================
    -- 2. Get the bookings (EXACTLY as original with cursors)
    -- =============================================
    DECLARE #Rescur CURSOR LOCAL FAST_FORWARD FOR
    SELECT DISTINCT 
    ConferenceRoomsXtab.CollectsConferenceRoomNumber, ConferenceRooms.Number, ConferenceRooms.Name, Translation.Name AS TranslatedHallName,
    CONVERT(DATETIME,ReservationConferenceLine.ArrivalDate,112) + CONVERT(DATETIME,ReservationConferenceLine.ArrivalTime,108) as Arrival,
    CONVERT(DATETIME,ReservationConferenceLine.DepartureDate,112) + CONVERT(DATETIME,ReservationConferenceLine.DepartureTime,108) as Departure,
    ReservationConferenceLine.ReferenceNumber, RESERVATIONCONFERENCELINE.ReservationConfLineNumber, Addr1.FirstName, Addr1.LASTNAME as MeetingName, 
    Addr1.ADDRESS1 as HostOwner, Addr2.ContactpersonCustomerNumber, Cust.FIRSTNAME + ' ' + Cust.LASTNAME as Contactperson, Addr2.LASTNAME as Payer,
    CASE p.UserTypeNumber WHEN 5 THEN 1 WHEN 9 THEN 1 ELSE 0 END as CreatedByOnline,
    IsNull(t.Name, Text) + (SELECT CASE WHEN TableSetupNote IS NOT NULL THEN ' (' + TableSetupNote + ')' ELSE '' END) AS TableSetting, 
    CASE WHEN ReservationConferenceLine.Plenum = 1 THEN
      ReservationConferenceLine.Adults + ReservationConferenceLine.Children1 + ReservationConferenceLine.Children2 
    ELSE 0 
    END as PAX
    ,ConferenceRooms.ConferenceRoomNumber
    ,RESERVATIONS.Status as ResStatus
      
    FROM ReservationConferenceLine WITH (NOLOCK)  
    INNER JOIN RESERVATIONS WITH (NOLOCK) ON RESERVATIONCONFERENCELINE.ReferenceNumber = RESERVATIONS.ReferenceNumber
    LEFT OUTER JOIN ReservationMain WITH (NOLOCK) ON ReservationMain.ReferenceNumber = Reservations.ReferenceNumber
    
    LEFT OUTER JOIN CONFERENCEROOMSXTAB WITH (NOLOCK) 
             ON ConferenceRoomsxtab.ConferenceRoomNumber = ReservationConferenceLine.ConferenceRoomNumber 
    LEFT OUTER JOIN ConferenceRooms WITH (NOLOCK) ON ConferenceRoomsXtab.CollectsConferenceRoomNumber = ConferenceRooms.ConferenceRoomNumber
    
    LEFT OUTER JOIN Translation WITH (NOLOCK) ON Translation.ObjectNumber = ConferenceRooms.ConferenceRoomNumber AND Translation.Type = 4 AND Translation.CountryNumber = 1
    INNER JOIN ReservationAddressXTab XTab1 WITH (NOLOCK) ON ReservationConferenceLine.ReferenceNumber = XTab1.ReferenceNumber 
                                                       AND ISNULL(ReservationConferenceLine.GuestNo, 0) = XTab1.GuestNumber 
                                                       AND XTab1.AddressTypeNumber = 1
    INNER JOIN ReservationAddress Addr1 WITH (NOLOCK) ON XTab1.AddressNumber = Addr1.AddressNumber

    LEFT JOIN ReservationAddressXTab Xtab2 WITH (NOLOCK) ON ISNULL(ReservationConferenceLine.GuestNo, 0 ) = Xtab2.GuestNumber
       AND Xtab2.ReferenceNumber = ReservationConferenceLine.ReferenceNumber 
       AND Xtab2.AddressTypeNumber = 2  
    LEFT JOIN ReservationAddress Addr2 WITH (NOLOCK) ON Addr2.AddressNumber = Xtab2.AddressNumber 
    LEFT JOIN Customer cust WITH (NOLOCK) on Addr2.ContactpersonCustomerNumber = cust.CustomerNumber 
    INNER JOIN Personel p WITH (NOLOCK) ON ReservationMain.StaffID = p.UserIDNumber
    LEFT OUTER JOIN CONFERENCEROOMTABLES ct on ReservationConferenceLine.ConferenceRoomSetupNumber = ct.ConferenceRoomTableNumber
    LEFT OUTER JOIN Translation t on t.ObjectNumber = ReservationConferenceLine.ConferenceRoomSetupNumber and t.Type = 5 and t.CountryNumber = @ACountryNumber
    
    WHERE 
    Reservations.GuestNumber = 0  AND 
    ISNULL(ReservationConferenceLine.SchemePattern, 0) = 0 AND Reservations.Status < 7
    AND  ConferenceRooms.AllowFilterPicassoOnline in (Select Number From iter_intlist_to_table(@AllowedFilterValues))  
    AND ((@AHotelNumber = -1) OR (@AHotelNumber > -1 AND ConferenceRooms.HotelNumber = @AHotelNumber ))
    AND CONVERT(DATETIME,ReservationConferenceLine.ArrivalDate,112) + CONVERT(DATETIME,ReservationConferenceLine.ArrivalTime,108) < DATEADD(MINUTE, ISNULL(ConferenceRooms.PauseInMinutes,0),@AEndDateTime )
    AND CONVERT(DATETIME,ReservationConferenceLine.DepartureDate,112) + CONVERT(DATETIME,ReservationConferenceLine.DepartureTime,108) > DATEADD(MINUTE,- ISNULL(ConferenceRooms.PauseInMinutes,0),@AStartDateTime)
    
    UNION
    --halls occupied via collects
    SELECT DISTINCT 
    ConferenceRoomsXtab.CollectsConferenceRoomNumber, CRooms2.Number, CRooms2.Name, Translation.Name AS TranslatedHallName,
    CONVERT(DATETIME,ReservationConferenceLine.ArrivalDate,112) + CONVERT(DATETIME,ReservationConferenceLine.ArrivalTime,108) as Arrival,
    CONVERT(DATETIME,ReservationConferenceLine.DepartureDate,112) + CONVERT(DATETIME,ReservationConferenceLine.DepartureTime,108) as Departure,
    ReservationConferenceLine.ReferenceNumber, RESERVATIONCONFERENCELINE.ReservationConfLineNumber, Addr1.FirstName, Addr1.LASTNAME as MeetingName, 
    Addr1.ADDRESS1 as HostOwner, Addr2.ContactpersonCustomerNumber, Cust.FIRSTNAME + ' ' + Cust.LASTNAME as Contactperson, Addr2.LASTNAME as Payer,
    CASE p.UserTypeNumber WHEN 5 THEN 1 WHEN 9 THEN 1 ELSE 0 END as CreatedByOnline,
    IsNull(t.Name, Text)  + (SELECT CASE WHEN TableSetupNote IS NOT NULL THEN ' (' + TableSetupNote + ')' ELSE '' END) AS TableSetting,
    CASE WHEN ReservationConferenceLine.Plenum = 1 THEN
      ReservationConferenceLine.Adults + ReservationConferenceLine.Children1 + ReservationConferenceLine.Children2 
    ELSE 0 
    END as PAX
    ,ConferenceRooms.ConferenceRoomNumber
    ,RESERVATIONS.Status as ResStatus
    
    FROM ReservationConferenceLine WITH (NOLOCK)  
    INNER JOIN RESERVATIONS WITH (NOLOCK) ON RESERVATIONCONFERENCELINE.ReferenceNumber = RESERVATIONS.ReferenceNumber
    LEFT OUTER JOIN ReservationMain WITH (NOLOCK) ON ReservationMain.ReferenceNumber = Reservations.ReferenceNumber
    
    INNER JOIN CONFERENCEROOMSXTAB WITH (NOLOCK) 
             ON ConferenceRoomsxtab.ConferenceRoomNumber = ReservationConferenceLine.ConferenceRoomNumber 
    INNER JOIN ConferenceRooms WITH (NOLOCK) ON ConferenceRoomsXtab.ConferenceRoomNumber = ConferenceRooms.ConferenceRoomNumber
    INNER JOIN ConferenceRooms CRooms2 WITH (NOLOCK) ON ConferenceRoomsXtab.CollectsConferenceRoomNumber = CRooms2.ConferenceRoomNumber
    
    LEFT OUTER JOIN Translation WITH (NOLOCK) ON Translation.ObjectNumber = CRooms2.ConferenceRoomNumber AND Translation.Type = 4 AND Translation.CountryNumber = 1
    INNER JOIN ReservationAddressXTab XTab1 WITH (NOLOCK) ON ReservationConferenceLine.ReferenceNumber = XTab1.ReferenceNumber 
                                                       AND ISNULL(ReservationConferenceLine.GuestNo, 0) = XTab1.GuestNumber 
                                                       AND XTab1.AddressTypeNumber = 1
    INNER JOIN ReservationAddress Addr1 WITH (NOLOCK) ON XTab1.AddressNumber = Addr1.AddressNumber

    LEFT JOIN ReservationAddressXTab Xtab2 WITH (NOLOCK) ON ISNULL(ReservationConferenceLine.GuestNo, 0 ) = Xtab2.GuestNumber
       AND Xtab2.ReferenceNumber = ReservationConferenceLine.ReferenceNumber 
       AND Xtab2.AddressTypeNumber = 2  
    LEFT JOIN ReservationAddress Addr2 WITH (NOLOCK) ON Addr2.AddressNumber = Xtab2.AddressNumber 
    LEFT JOIN Customer cust WITH (NOLOCK) on Addr2.ContactpersonCustomerNumber = cust.CustomerNumber 
    INNER JOIN Personel p WITH (NOLOCK) ON ReservationMain.StaffID = p.UserIDNumber
    LEFT OUTER JOIN CONFERENCEROOMTABLES ct on ReservationConferenceLine.ConferenceRoomSetupNumber = ct.ConferenceRoomTableNumber
    LEFT OUTER JOIN Translation t on t.ObjectNumber = ReservationConferenceLine.ConferenceRoomSetupNumber and t.Type = 5 and t.CountryNumber = @ACountryNumber
    WHERE 
    Reservations.GuestNumber = 0  AND 
    ISNULL(ReservationConferenceLine.SchemePattern, 0) = 0 AND Reservations.Status < 7
    AND  CRooms2.AllowFilterPicassoOnline in (Select Number From iter_intlist_to_table(@AllowedFilterValues))  
    AND ((@AHotelNumber = -1) OR (@AHotelNumber > -1 AND CRooms2.HotelNumber = @AHotelNumber ))
    AND CONVERT(DATETIME,ReservationConferenceLine.ArrivalDate,112) + CONVERT(DATETIME,ReservationConferenceLine.ArrivalTime,108) < DATEADD(MINUTE, ISNULL(CRooms2.PauseInMinutes,0),@AEndDateTime )
    AND CONVERT(DATETIME,ReservationConferenceLine.DepartureDate,112) + CONVERT(DATETIME,ReservationConferenceLine.DepartureTime,108) > DATEADD(MINUTE,- ISNULL(CRooms2.PauseInMinutes,0),@AStartDateTime)
    
    UNION
    --Out of order (MeetingName="ooo")
    SELECT DISTINCT 
    ConferenceRoomsXtab.CollectsConferenceRoomNumber, ConferenceRooms.Number, ConferenceRooms.Name, Translation.Name AS TranslatedHallName,
    CONVERT(DATETIME,ReservationConferenceLine.ArrivalDate,112) + CONVERT(DATETIME,ReservationConferenceLine.ArrivalTime,108) as Arrival,
    CONVERT(DATETIME,ReservationConferenceLine.DepartureDate,112) + CONVERT(DATETIME,ReservationConferenceLine.DepartureTime,108) as Departure,
    0, RESERVATIONCONFERENCELINE.ReservationConfLineNumber, '', 'ooo' as MeetingName, 
    '' as HostOwner, 0, '' as Contactperson, '' as Payer,
    0 as CreatedByOnline,
    '' AS TableSetting,
    0 as PAX
    ,ConferenceRooms.ConferenceRoomNumber
    ,NULL as ResStatus
    
    FROM ReservationConferenceLine WITH (NOLOCK)  
    INNER JOIN CONFERENCEROOMSXTAB WITH (NOLOCK) 
             ON ConferenceRoomsxtab.ConferenceRoomNumber = ReservationConferenceLine.ConferenceRoomNumber 
    INNER JOIN ConferenceRooms WITH (NOLOCK) ON ConferenceRoomsXtab.CollectsConferenceRoomNumber = ConferenceRooms.ConferenceRoomNumber
    
    LEFT OUTER JOIN Translation WITH (NOLOCK) ON Translation.ObjectNumber = ConferenceRooms.ConferenceRoomNumber AND Translation.Type = 4 AND Translation.CountryNumber = 1
    WHERE 
    ReservationConferenceLine.ReferenceNumber = 0 AND
    ISNULL(ReservationConferenceLine.SchemePattern, 0) = 0
    AND  ConferenceRooms.AllowFilterPicassoOnline in (Select Number From iter_intlist_to_table(@AllowedFilterValues))  
    AND ((@AHotelNumber = -1) OR (@AHotelNumber > -1 AND ConferenceRooms.HotelNumber = @AHotelNumber ))
    AND CONVERT(DATETIME,ReservationConferenceLine.ArrivalDate,112) + CONVERT(DATETIME,ReservationConferenceLine.ArrivalTime,108) < DATEADD(MINUTE, ISNULL(ConferenceRooms.PauseInMinutes,0),@AEndDateTime )
    AND CONVERT(DATETIME,ReservationConferenceLine.DepartureDate,112) + CONVERT(DATETIME,ReservationConferenceLine.DepartureTime,108) > DATEADD(MINUTE,- ISNULL(ConferenceRooms.PauseInMinutes,0),@AStartDateTime)
    
    OPEN #Rescur
    FETCH NEXT FROM #Rescur 
    INTO @HallNumberId, @HallNumber, @HallName, @TranslatedHallName, 
         @ArrivalDate, @DepartureDate, 
         @ReferenceNumber, @RoomLineNumber, @FirstName, @MeetingName, 
         @HostOwner, @ContactpersonCustomerNumber, @Contactperson, @Payer,
         @CreatedByOnline,
         @TableSetting,
         @PAX
         ,@ConferenceRoomNumber,
         @ResStatus
    WHILE (@@FETCH_STATUS <> -1)
    BEGIN
        IF LTRIM(RTRIM(@TranslatedHallName)) IS NOT NULL
          SET @HallName = @TranslatedHallName
          
        --Subtypes
        Declare @found1 int
        IF @SubTypeTxt <> ''
        BEGIN
          SELECT @found1 = COUNT(*) 
          FROM conferenceroomsubtypes crst WITH (NOLOCK)
          INNER JOIN conferenceroomsubtyperoomsextab crx WITH (NOLOCK) on crst.ConferenceRoomSubTypeNumber=crx.ConferenceRoomSubTypeNumber
          WHERE crx.ConferenceRoomNumber = @HallNumberID and crst.Text = @SubTypeTxt
        END ELSE
        BEGIN
          Set @found1 = 1
        END
        
        IF @found1 <> 0
        BEGIN
          INSERT INTO #BookedHalls (HallNumberID, HallNumber, HallName, ArrivalDate, DepartureDate, ReferenceNumber, RoomLineNumber, FirstName, LastName, MeetingName, HostOwner, ContactpersonCustomerNumber, Contactperson, Payer, CreatedByOnline,
                          TableSetting, PAX, ConferenceRoomNumber, ResStatus) 
          VALUES (@HallNumberID,@HallNumber, @HallName, @ArrivalDate, @DepartureDate, @ReferenceNumber, @RoomLineNumber, @FirstName, @LastName, @MeetingName, @HostOwner, @ContactpersonCustomerNumber, @Contactperson, @Payer, @CreatedByOnline,
                  @TableSetting, @PAX, @ConferenceRoomNumber, @ResStatus)
        END
      FETCH NEXT FROM #Rescur
      INTO @HallNumberId, @HallNumber, @HallName, @TranslatedHallName, 
           @ArrivalDate, @DepartureDate, 
           @ReferenceNumber, @RoomLineNumber, @FirstName, @MeetingName, 
           @HostOwner, @ContactpersonCustomerNumber, @Contactperson, @Payer,
           @CreatedByOnline,
           @TableSetting,
           @PAX
           ,@ConferenceRoomNumber,
           @ResStatus
    END
    CLOSE #Rescur
    DEALLOCATE #Rescur

    -- Fill HallMaxPax into the booking-records from the halls records (EXACTLY as original)
    DECLARE #BookedHallsCur CURSOR LOCAL FAST_FORWARD FOR
    SELECT HallNumberId, HallMaxPax 
    FROM #BookedHalls WITH (NOLOCK)
    WHERE ISNULL(HallMaxPax, -1) > -1
    
    OPEN #BookedHallsCur
    FETCH NEXT FROM #BookedHallsCur INTO @HallNumberId, @HallTypeMaxPax
    WHILE (@@FETCH_STATUS <> -1)
    BEGIN
        --Find 1. picture
        Declare @Picture VarChar(100);
        Set @Picture = '';
        Select Top 1 @Picture = ImageName From OnlineHallImagesXTab (nolock) 
        Where ConferenceRoomNumber = @HallNumberId 
        Order By SortOrder

        UPDATE #BookedHalls Set HallMaxPax = @HallTypeMaxPax, HallImage = @Picture 
        WHERE HallNumberId = @HallNumberId
        
        FETCH NEXT FROM #BookedHallsCur INTO @HallNumberId, @HallTypeMaxPax
    END
    CLOSE #BookedHallsCur
    DEALLOCATE #BookedHallsCur
    
    SELECT HallNumberId, HallNumber, HallName, HallMaxPax, HallDescription, PicassoFilterOnline, OnlineAllowBook,
           ArrivalDate, DepartureDate, ReferenceNumber, RoomLineNumber, FirstName, LastName, MeetingName,
           HostOwner, ContactpersonCustomerNumber, ContactPerson, Payer,
           CreatedByOnline, HallImage, TableSetting, PAX, ConferenceRoomNumber, ResStatus as ReservationStatus
    FROM  #BookedHalls WITH (NOLOCK) 
    
    ORDER BY CASE @OrderBy 
               WHEN 'HallNumber'   THEN REPLACE(STR(HallNumber, 3),' ','0')
               WHEN 'HallName'     THEN HallName
               WHEN 'MaxPax'       Then REPLACE(STR(HallMaxPax, 3),' ','0')    
             END  
             ,HallNumberID, ArrivalDate
    DROP TABLE #BookedHalls;
END

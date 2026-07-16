// =============================================
// REFACTORED HOTEL SETUP LOADER
// Complete replacement for the original nested if-statement logic
// =============================================

using System;
using System.Collections.Generic;
using System.Data.SqlClient;

public static class HotelSetupLoader
{
    // Helper class for parsing common value types
    private static class Parser
    {
        public static bool ToBool(string v) => v == "1";
        public static bool ToBoolNotZero(string v) => v != "0";
        public static int ToInt(string v, int defaultValue = 0) =>
            int.TryParse(v, out int result) ? result : defaultValue;
        public static string NullIfEmpty(string v) => string.IsNullOrEmpty(v) ? null : v;
        public static T ToEnum<T>(string v, T defaultValue) where T : struct =>
            Enum.TryParse(v, true, out T result) ? result : defaultValue;
    }

    // Complete mapping dictionary for all settings
    // Key: searchkey from database (case-insensitive)
    // Value: Action to set the corresponding property on HotelSetupSearch
    private static readonly Dictionary<string, Action<HotelSetupSearch, string, int, SendMailService>>
        _settingMappings = new Dictionary<string, Action<HotelSetupSearch, string, int, SendMailService>>
        (StringComparer.OrdinalIgnoreCase)
    {
        // =============================================
        // BOOLEAN SETTINGS (1 = true, others = false)
        // =============================================
        ["PicassoOnlineIncludeDynamicTopTextInConfirmationMail"] = (s, v, h, sm) =>
            s.IncludeDynamicTopTextInConfirmationMail = Parser.ToBool(v),
        ["PicassoOnlineUseCheapProduct"] = (s, v, h, sm) =>
            s.UseCheapProduct = Parser.ToBool(v),
        ["PicassoOnlineShowBookMoreRoomFrame"] = (s, v, h, sm) =>
            s.ShowBookMoreRoomFrame = Parser.ToBoolNotZero(v),
        ["PicassoOnlineBookMoreRooms"] = (s, v, h, sm) =>
            s.BookMoreRooms = Parser.ToBoolNotZero(v),
        ["PicassoOnlineShowChildren2"] = (s, v, h, sm) =>
            s.ShowChildren2 = Parser.ToBool(v),
        ["PicassoOnlineShowFirstBeforeLastname"] = (s, v, h, sm) =>
            s.ShowFirstBeforeLastname = Parser.ToBool(v),
        ["OnlineWareDelArrDepPayArr"] = (s, v, h, sm) =>
            s.WareTypeDelArrDepPayArr = Parser.ToBool(v) ? 1 : 0,
        ["OnlineRightJustifiedShopcart"] = (s, v, h, sm) =>
            s.RightJustifiedShopcart = Parser.ToBoolNotZero(v),
        ["OnlineShowOneColumnDateWare"] = (s, v, h, sm) =>
            s.ShowOneColumnDateWare = Parser.ToBool(v),
        ["OnlineShowAdultAndChildGrid"] = (s, v, h, sm) =>
            s.ShowAdultAndChildrenInGrid = Parser.ToBool(v),
        ["OnlineUseHouseNotRoom"] = (s, v, h, sm) =>
            s.UseHouseNotRoom = Parser.ToBool(v),
        ["OnlineHeaderAdultChildPrRoom"] = (s, v, h, sm) =>
            s.HeaderAdultChildPrRoom = Parser.ToBoolNotZero(v),
        ["OnlineShowHeaderForBoughtWare"] = (s, v, h, sm) =>
            s.ShowHeaderForBoughtWare = Parser.ToBool(v),
        ["OnlineShowProductCellInGrid"] = (s, v, h, sm) =>
            s.ShowProductCellInGrid = Parser.ToBoolNotZero(v),
        ["OnlineUseEniroSearch"] = (s, v, h, sm) =>
            s.UseEniroSearch = Parser.ToBool(v),
        ["EniroSearchEnabled"] = (s, v, h, sm) =>
            s.EniroSearchEnabled = Parser.ToBool(v),
        ["ForceOnSegment"] = (s, v, h, sm) =>
            s.ForceOnSegment = Parser.ToBool(v),
        ["ForceOnPOS"] = (s, v, h, sm) =>
            s.ForceOnPOS = Parser.ToBool(v),
        ["PurposeCode"] = (s, v, h, sm) =>
            s.ForceOnPurposeCode = Parser.ToBool(v),
        ["GuestType"] = (s, v, h, sm) =>
            s.ForceOnGuestType = Parser.ToBool(v),
        ["OnlineShowNoteTextReminder"] = (s, v, h, sm) =>
            s.ShowNoteTextReminder = Parser.ToBool(v),
        ["OnlineShowNotesWares"] = (s, v, h, sm) =>
            s.ShowNoteWares = Parser.ToBool(v),
        ["OnlineUseTranslation"] = (s, v, h, sm) =>
            s.UseTranslation = Parser.ToBool(v),
        ["OnlineUseNewWellnessInterface"] = (s, v, h, sm) =>
            s.OnlineUseNewWellnessInterface = Parser.ToBool(v),
        ["OnlineHallAllowConfirmNoCard"] = (s, v, h, sm) =>
            s.AllowConfirmWithoutPaymentHall = Parser.ToBool(v),
        ["OnlineHallActivate"] = (s, v, h, sm) =>
            s.HallActivate = Parser.ToBool(v),
        ["OnlineShowAvailableHalls"] = (s, v, h, sm) =>
            s.ShowAvailableHalls = Parser.ToBool(v),
        ["OnlineRoomAllowConfirmNoCard"] = (s, v, h, sm) =>
            s.AllowConfirmWithoutPaymentRoom = Parser.ToBool(v),
        ["OnlineHallEnableCustomerLogin"] = (s, v, h, sm) =>
            s.HallEnableCustomerLogin = Parser.ToBool(v),
        ["OnlineHallEnableCalendarView"] = (s, v, h, sm) =>
            s.HallEnableCalenderView = Parser.ToBool(v),
        ["OnlineShowEAN"] = (s, v, h, sm) =>
            s.ShowEANAccountInvoiceNo = Parser.ToBool(v),
        ["OnlineShowProDescOnLastPage"] = (s, v, h, sm) =>
            s.ShowProductDescriptionLastPage = Parser.ToBool(v),
        ["ForceOnContactPerson"] = (s, v, h, sm) =>
            s.ContactIsRequired = Parser.ToBool(v),
        ["OnlineUseCustomerLogin"] = (s, v, h, sm) =>
            s.UseCustomerLogin = Parser.ToBool(v),
        ["OnlineShowCustomerAtLogin"] = (s, v, h, sm) =>
            s.ShowCustomerAtLogin = Parser.ToBool(v),
        ["OnlineCourtAddToReservation"] = (s, v, h, sm) =>
            s.CourtAddToReservation = Parser.ToBool(v),
        ["OnlineHourRoomBooking"] = (s, v, h, sm) =>
            s.UseHourRoomBooking = Parser.ToBool(v),
        ["OnlineShowCalender"] = (s, v, h, sm) =>
            s.UseCalenderDate = Parser.ToBool(v),
        ["OnlineShowAvailableCourts"] = (s, v, h, sm) =>
            s.ShowAvailableCourts = Parser.ToBoolNotZero(v),
        ["OnlineCourtOnlyShowStartTime"] = (s, v, h, sm) =>
            s.CourtOnlyShowStartTime = Parser.ToBool(v),
        ["OnlineCourtPaymentForAddToReservation"] = (s, v, h, sm) =>
            s.CourtPaymentForAddToReservation = Parser.ToBool(v),
        ["OnlineCourtUseEniroSearch"] = (s, v, h, sm) =>
            s.CourtUseEniroSearch = Parser.ToBool(v),
        ["OnlineCourtEnableBookingOnMainReservation"] = (s, v, h, sm) =>
            s.CourtEnableBookingOnMainReservation = Parser.ToBool(v),
        ["OnlineHallShowTypeColoumn"] = (s, v, h, sm) =>
            s.HallShowTypeColoumn = Parser.ToBoolNotZero(v),
        ["OnlineHallShowNumberColumn"] = (s, v, h, sm) =>
            s.OnlineHallShowNumberColumn = Parser.ToBoolNotZero(v),
        ["PicassoOnlineUseCancellationFee"] = (s, v, h, sm) =>
            s.UseCancellationFee = Parser.ToBoolNotZero(v),
        ["OnlineUseMultiAvailGrid"] = (s, v, h, sm) =>
            s.UseMultiAvailGrid = Parser.ToBool(v),
        ["OnlineShowLogoutButton"] = (s, v, h, sm) =>
            s.ShowLogoutButton = Parser.ToBool(v),
        ["OnlineCourtUseDayView"] = (s, v, h, sm) =>
            s.CourtsUseDayView = Parser.ToBool(v),
        ["OnlineCancellFeeMandatory"] = (s, v, h, sm) =>
            s.CancellFeeIsMandatory = Parser.ToBool(v),
        ["OnlineUseAgentcyFirmLogin"] = (s, v, h, sm) =>
            s.UseAgentcyFirmLogin = Parser.ToBool(v),
        ["PicassoOnlineAddressMandatory"] = (s, v, h, sm) =>
            s.AddressMandatory = Parser.ToBool(v),
        ["OnlineShowPaxAtCalander"] = (s, v, h, sm) =>
            s.ShowPaxWithCalander = Parser.ToBoolNotZero(v),
        ["OnlineAvailCalendarArrKlik"] = (s, v, h, sm) =>
            s.SearchAtArrivalKlik = Parser.ToBool(v),
        ["OnlineAvailCalendarButKlik"] = (s, v, h, sm) =>
            s.SearchAtButtonKlik = Parser.ToBoolNotZero(v),
        ["onlineAvailCalendarDepKlik"] = (s, v, h, sm) =>
            s.SearchAtDepKlik = Parser.ToBool(v),
        ["OnlineShowChooseNewDate"] = (s, v, h, sm) =>
            s.ShowChooseNewDate = Parser.ToBoolNotZero(v),
        ["OnlineShowPlusMinusDays"] = (s, v, h, sm) =>
            s.ShowPlusMinuDays = Parser.ToBoolNotZero(v),
        ["OnlineShowRoomsAtCalendar"] = (s, v, h, sm) =>
            s.ShowNumRoomsAtCalander = Parser.ToBoolNotZero(v),
        ["onlineusecreditcard"] = (s, v, h, sm) =>
            s.UseCreditCardPayment = Parser.ToBoolNotZero(v),
        ["onlineTerminalOnly1Room"] = (s, v, h, sm) =>
            s.TerminalOnly1Room = Parser.ToBoolNotZero(v),
        ["onlineEnviromentFeeMandatory"] = (s, v, h, sm) =>
            s.EnviromentFeeMandatory = Parser.ToBool(v),
        ["CalcEnvironmentFeeViaArrangementLine"] = (s, v, h, sm) =>
            s.CalcEnvironmentFeeViaArrangementLine = Parser.ToBoolNotZero(v),
        ["OnlineHallShowName"] = (s, v, h, sm) =>
            s.OnlineHallShowName = Parser.ToBool(v),
        ["OnlineHallUseVoucher"] = (s, v, h, sm) =>
            s.OnlineHallUseVoucher = Parser.ToBool(v),
        ["OnlineHallShowNetworkSelectionOnNewCustomerPage"] = (s, v, h, sm) =>
            s.OnlineHallShowNetworkSelectionOnNewCustomerPage = Parser.ToBool(v),
        ["OnlineAllowbookonly1room"] = (s, v, h, sm) =>
            s.AllowBookOnly1Room = Parser.ToBool(v),
        ["OnlineShowOnlyArrivalCalendar"] = (s, v, h, sm) =>
            s.ShowOnlyArrivalCalendar = Parser.ToBool(v),
        ["OnlineContactsShowFullName"] = (s, v, h, sm) =>
            s.OnlineContactsShowFullName = Parser.ToBool(v),
        ["OnlineShowDeliveryTimeOnWare"] = (s, v, h, sm) =>
            s.OnlineShowDeliveryTimeOnWare = Parser.ToBool(v),
        ["OnlineUseRoomNumberbooking"] = (s, v, h, sm) =>
            s.UseRoomNumberbooking = Parser.ToBool(v),
        ["OnlineShowPacketContents"] = (s, v, h, sm) =>
            s.OnlineShowPacketContents = Parser.ToBool(v),
        ["OnlineShowMinPrice"] = (s, v, h, sm) =>
            s.ShowMinPrice = Parser.ToBool(v),
        ["OnlineShowGNSPrice"] = (s, v, h, sm) =>
            s.ShowGNSPrice = Parser.ToBool(v),
        ["OnlineBookOwnHouse"] = (s, v, h, sm) =>
            s.BookOwnHouse = Parser.ToBool(v),
        ["OnlineAddDemandedArrangement"] = (s, v, h, sm) =>
            s.AddDemandedArrangement = Parser.ToBool(v),
        ["OnlineUselocalValidateCreditcard"] = (s, v, h, sm) =>
            s.UselocalValidateCreditcard = Parser.ToBool(v),
        ["OnlineAutoselect1product"] = (s, v, h, sm) =>
            s.AutoSelect1Product = Parser.ToBool(v),
        ["UseDynamicPaymentControl"] = (s, v, h, sm) =>
            s.UseDynamicPaymentControl = Parser.ToBool(v),
        ["OnlineForceOnGuestMailAtFirmBooking"] = (s, v, h, sm) =>
            s.ForceOnGuestMailAtFirmBooking = Parser.ToBool(v),
        ["OnlineUseDynamicDemandedWares"] = (s, v, h, sm) =>
            s.UseDynamicDemandedWares = Parser.ToBool(v),
        ["OnlineShowCurrencyTextWithAvailability"] = (s, v, h, sm) =>
            s.ShowCurrencyTextWithAvailability = Parser.ToBoolNotZero(v),
        ["OnlineAlwaysAllowBookingWithoutCreditcard"] = (s, v, h, sm) =>
            s.AlwaysAllowReservationWithoutCreditcard = Parser.ToBool(v),
        ["OnlineShowPriceEUR"] = (s, v, h, sm) =>
            s.ShowPriceEUR = Parser.ToBool(v),
        ["OnlineFindHouseAddressFromRoomtype"] = (s, v, h, sm) =>
            s.UseHouseAddressFromRoomtypes = Parser.ToBool(v),
        ["OnlineShowDemandedWaresInCart"] = (s, v, h, sm) =>
            s.ShowDemandedWaresInCart = Parser.ToBool(v),
        ["OnlinePricesInclVat"] = (s, v, h, sm) =>
            s.PricesInclVat = Parser.ToBoolNotZero(v),
        ["OnlineOptionalWareOndefault"] = (s, v, h, sm) =>
            s.OptionalWareRejectionOndefault = Parser.ToBool(v),
        ["Onlinepaintarrivaldayscalendar"] = (s, v, h, sm) =>
            s.PaintArrivalDaysCalendar = Parser.ToBool(v),
        ["OnlineShowDatePopup"] = (s, v, h, sm) =>
            s.ShowDatePopup = Parser.ToBool(v),
        ["OnlineShowUserCounter"] = (s, v, h, sm) =>
            s.ShowUserCounter = Parser.ToBool(v),
        ["OnlineUsepatienthotelmode"] = (s, v, h, sm) =>
            s.UsePatienthotelmode = Parser.ToBool(v),
        ["OnlineUseBookParm"] = (s, v, h, sm) =>
            s.UseBookParm = Parser.ToBool(v),
        ["OnlineSaveRestypeAltRes"] = (s, v, h, sm) =>
            s.SaveResTypeAltRes = Parser.ToBool(v),
        ["OnlineShowPersonNoAdult"] = (s, v, h, sm) =>
            s.ShowPersonNoAdult = Parser.ToBool(v),
        ["onlineonlyvalidatecreditcard"] = (s, v, h, sm) =>
            s.OnlyValidateCreditcard = Parser.ToBool(v),
        ["OnlineUseNewsOnCustomer"] = (s, v, h, sm) =>
            s.UseNewsOnCustomer = Parser.ToBool(v),
        ["OnlineUseOptionalwarePerRoomtype"] = (s, v, h, sm) =>
            s.UseOptionalwarePerRoomtype = Parser.ToBool(v),
        ["OnlineSortDeliveryTypeDescending"] = (s, v, h, sm) =>
            s.SortDeliveryTypeDescending = Parser.ToBool(v),
        ["OnlineShowonlyarrdatescalendar"] = (s, v, h, sm) =>
            s.ShowOnlyAvailDatesCalendar = Parser.ToBool(v),
        ["OnlineUseSectorAsLocation"] = (s, v, h, sm) =>
            s.UseSectorAsLocation = Parser.ToBool(v),
        ["OnlineWare1stSortSortnumber"] = (s, v, h, sm) =>
            s.Ware1stSortSortnumber = Parser.ToBool(v),
        ["OnlineServiceCancelType"] = (s, v, h, sm) =>
            s.UseStatusTable = Parser.ToBool(v),
        ["OnlineUsecustomermenu"] = (s, v, h, sm) =>
            s.UseCustomerMenu = Parser.ToBool(v),
        ["OnlineUseDevxCalendar"] = (s, v, h, sm) =>
            s.UseCalendarDevExp = Parser.ToBool(v),
        ["OnlineUseProductWareRelation"] = (s, v, h, sm) =>
            s.UseProductWareRelation = Parser.ToBool(v),
        ["OnlineHallDisableParticipants"] = (s, v, h, sm) =>
            s.HallDisableParticipants = Parser.ToBoolNotZero(v),
        ["OnlineHallDisableEndDateWhenSelect"] = (s, v, h, sm) =>
            s.OnlineHallDisableEndDateWhenSelect = Parser.ToBoolNotZero(v),
        ["OnlineShowCancelTableBooking"] = (s, v, h, sm) =>
            s.OnlineShowCancelTableBooking = Parser.ToBoolNotZero(v),
        ["OnlineShowMealType"] = (s, v, h, sm) =>
            s.OnlineShowMealType = Parser.ToBoolNotZero(v),
        ["OnlineEnableSelectTableType"] = (s, v, h, sm) =>
            s.EnableSelectTableType = Parser.ToBoolNotZero(v),
        ["OnlineUseGifttokenPayment"] = (s, v, h, sm) =>
            s.OnlineUseGifttokenPayment = Parser.ToBoolNotZero(v),
        ["OnlineShowCampaignCode"] = (s, v, h, sm) =>
            s.OnlineShowCampaignCode = Parser.ToBoolNotZero(v),
        ["OnlineShowTableBookingInfoText"] = (s, v, h, sm) =>
            s.OnlineShowTableBookingInfoText = Parser.ToBoolNotZero(v),
        ["OnlineShowTableBookingTopImage"] = (s, v, h, sm) =>
            s.OnlineShowTableBookingTopImage = Parser.ToBoolNotZero(v),
        ["onlineusecreditcardfee"] = (s, v, h, sm) =>
            s.UseCreditcardFee = Parser.ToBoolNotZero(v),
        ["onlineterminalpaymentbeforecheckin"] = (s, v, h, sm) =>
            s.Terminalpaymentbeforecheckin = Parser.ToBoolNotZero(v),
        ["onlineuseonlinegold"] = (s, v, h, sm) =>
            s.UseOnlineGold = Parser.ToBoolNotZero(v),
        ["OnlineUseMultiAvailGrid"] = (s, v, h, sm) =>
            s.UseMultiAvailGrid = Parser.ToBool(v),
        ["OnlineRoomTypeTextPerPAX"] = (s, v, h, sm) =>
            s.UsedynamicPaxText = Parser.ToBoolNotZero(v),
        ["onlinefirstlastname2lines"] = (s, v, h, sm) =>
            s.ShowFirstLastname2Lines = Parser.ToBoolNotZero(v),
        ["onlinegoldlargeproductpicture"] = (s, v, h, sm) =>
            s.UseGoldlargeproductpicture = Parser.ToBoolNotZero(v),
        ["onlinegolddominotopv2"] = (s, v, h, sm) =>
            s.UseGolddominotopv2 = Parser.ToBoolNotZero(v),
        ["onlineaddconfirmtojournal"] = (s, v, h, sm) =>
            s.AddConfirmLetterToJournal = Parser.ToBoolNotZero(v),
        ["onlinestayloggedinafterconfirm"] = (s, v, h, sm) =>
            s.CustomerStayLoginAfterConfirm = Parser.ToBoolNotZero(v),
        ["onlineusepatienthotelgold"] = (s, v, h, sm) =>
            s.UsePatientBookingGold = Parser.ToBoolNotZero(v),
        ["onlineusecalendaravailability"] = (s, v, h, sm) =>
            s.UseCalendarAvailability = Parser.ToBoolNotZero(v),
        ["onlineshowactivitycodeinmail"] = (s, v, h, sm) =>
            s.ShowActivityNoteOnConfirmMail = Parser.ToBoolNotZero(v),
        ["onlineusecustomerlogindominotopv2"] = (s, v, h, sm) =>
            s.UseCustomerLoginWithDominoTopV2 = Parser.ToBoolNotZero(v),
        ["onlineusedominoavailability"] = (s, v, h, sm) =>
            s.UseDominoAvailability = Parser.ToBoolNotZero(v),
        ["onlineusedominoflowcache"] = (s, v, h, sm) =>
            s.UseDominoFlowCache = Parser.ToBoolNotZero(v),
        ["IsDanHostel"] = (s, v, h, sm) =>
            s.IsDanHostel = Parser.ToBoolNotZero(v),
        ["OnlineTerminalDetailedInterfaceResponse"] = (s, v, h, sm) =>
            s.IsTerminalDetailedInterface = Parser.ToBool(v),
        ["OnlineCheckoutDontPostDeposit"] = (s, v, h, sm) =>
            s.CheckoutDontPostDeposit = Parser.ToBool(v),
        ["onlineemptycartonotherhotelsearch"] = (s, v, h, sm) =>
            s.Emptycartonotherhotelsearch = Parser.ToBool(v),
        ["onlineusepictureonextra"] = (s, v, h, sm) =>
            s.UsePictureOnExtra = Parser.ToBool(v),
        ["onlineallowusertochooseroomnumber"] = (s, v, h, sm) =>
            s.AllowUserToChooseRoomNumber = Parser.ToBool(v),
        ["onlineusecurrencybycountrycode"] = (s, v, h, sm) =>
            s.UseCurrencyByCountryCode = Parser.ToBool(v),
        ["onlineusedanhostelmembercard"] = (s, v, h, sm) =>
            s.UseDanhostelMemberCard = Parser.ToBool(v),
        ["onlineuselargepax"] = (s, v, h, sm) =>
            s.UseLargePax = Parser.ToBool(v),
        ["onlineuseexternallink"] = (s, v, h, sm) =>
            s.UseExternalLink = Parser.ToBool(v),
        ["OnlineShowPricesForAllPax"] = (s, v, h, sm) =>
            s.OnlineShowPricesForAllPax = v.Contains("1"),
        ["OnlineLoginWithPwdParm"] = (s, v, h, sm) =>
            s.LoginWithPwdParm = Parser.ToBool(v),
        ["OnlineUseSectorAsLocation"] = (s, v, h, sm) =>
            s.UseSectorAsLocation = Parser.ToBool(v),
        ["OnlineSplitPaymentOnCreditCard"] = (s, v, h, sm) =>
            s.UsePaymentOnDifferentAccounts = Parser.ToBoolNotZero(v),
        ["onlinebookonlypersons"] = (s, v, h, sm) =>
            s.BookOnlyPersons = Parser.ToBoolNotZero(v),
        ["onlineuseuservoice"] = (s, v, h, sm) =>
            s.UseUserVoiceScript = Parser.ToBoolNotZero(v),
        ["onlineusepicassoonlineservice"] = (s, v, h, sm) =>
            s.UsePicassoOnlineService = Parser.ToBoolNotZero(v),
        ["OnlineRoomTypeTextPerPAX"] = (s, v, h, sm) =>
            s.UsedynamicPaxText = Parser.ToBoolNotZero(v),
        ["onlineusecreditcardfee"] = (s, v, h, sm) =>
            s.UseCreditcardFee = Parser.ToBoolNotZero(v),
        ["onlineterminalpaymentbeforecheckin"] = (s, v, h, sm) =>
            s.Terminalpaymentbeforecheckin = Parser.ToBoolNotZero(v),
        ["onlineuseonlinegold"] = (s, v, h, sm) =>
            s.UseOnlineGold = Parser.ToBoolNotZero(v),
        ["onlineusegoogletagmanager"] = (s, v, h, sm) =>
            s.UseGooglTagManager = Parser.ToBoolNotZero(v),
        ["onlineusecampaigncoderoom"] = (s, v, h, sm) =>
            s.UseCampaignCodeRoom = Parser.ToBoolNotZero(v),
        ["onlineusecustomerselectcurrency"] = (s, v, h, sm) =>
            s.UseCustomerSelectCurrency = Parser.ToBoolNotZero(v),
        ["onlineusevisualcurrency"] = (s, v, h, sm) =>
            s.UseVisualCurrency = Parser.ToBoolNotZero(v),
        ["onlinebookwithzeropriceperdayfromproduct"] = (s, v, h, sm) =>
            s.Bookwithzeropriceperdayfromproduct = Parser.ToBoolNotZero(v),
        ["onlineusesegmentposfromproduct"] = (s, v, h, sm) =>
            s.UseSegmentPosFromProduct = Parser.ToBoolNotZero(v),
        ["HallClimbingUseClimbingAvailability"] = (s, v, h, sm) =>
            s.HallClimbingUseClimbingAvailability = Parser.ToBoolNotZero(v),
        ["onlineusecookiepopuptest"] = (s, v, h, sm) =>
            s.UseCookiePopupTest = Parser.ToBoolNotZero(v),
        ["onlineusepdfmail"] = (s, v, h, sm) =>
            s.UseConfirmpdfMail = Parser.ToBoolNotZero(v),
        ["onlineshowemailsearchcustomer"] = (s, v, h, sm) =>
            s.ShowEmailSearchCustomer = Parser.ToBoolNotZero(v),
        ["onlinebookmoreroomsgold"] = (s, v, h, sm) =>
            s.BookMoreRoomsGold = Parser.ToBoolNotZero(v),
        ["onlineusecalendaravailability"] = (s, v, h, sm) =>
            s.UseCalendarAvailability = Parser.ToBoolNotZero(v),
        ["onlineuseuservoice"] = (s, v, h, sm) =>
            s.UseUserVoiceScript = Parser.ToBoolNotZero(v),
        ["onlineusepicassoonlineservice"] = (s, v, h, sm) =>
            s.UsePicassoOnlineService = Parser.ToBoolNotZero(v),
        ["onlineuseuservoice"] = (s, v, h, sm) =>
            s.UseUserVoiceScript = Parser.ToBoolNotZero(v),

        // =============================================
        // STRING SETTINGS (Direct assignment)
        // =============================================
        ["DatabaseVersionNumber"] = (s, v, h, sm) =>
            s.DatabaseVersionNumber = Parser.NullIfEmpty(v),
        ["TimeOfArrivalForRes"] = (s, v, h, sm) =>
            s.ArriveTimeRes = v,
        ["TimeOfArrivalForDayRoom"] = (s, v, h, sm) =>
            s.ArriveTimeDayRoom = v,
        ["TimeOfDepartureForRes"] = (s, v, h, sm) =>
            s.DepartureTimeRes = v,
        ["TimeOfDepartureForDayRoom"] = (s, v, h, sm) =>
            s.DepartureTimeDayRoom = v,
        ["OnlineUrlLinkToFront"] = (s, v, h, sm) =>
            s.UrlLinkToFront = v,
        ["OnlineUrlLinkToNewsletter"] = (s, v, h, sm) =>
            s.UrlLinkToNewsletter = v,
        ["EniroSearch_BaseURL"] = (s, v, h, sm) =>
            s.EniroSearchBaseURL = v,
        ["EniroSearch_PinCode"] = (s, v, h, sm) =>
            s.EniroSearchPinCode = v,
        ["EniroSearchIdent_IndType"] = (s, v, h, sm) =>
            s.EniroSearchIdentIndType = v,
        ["EniroSearchIdent_Phone"] = (s, v, h, sm) =>
            s.EniroSearchIdentPhone = v,
        ["EniroSearchIdent_PinCode"] = (s, v, h, sm) =>
            s.EniroSearchIdentPinCode = v,
        ["OnlineHallOpenTime"] = (s, v, h, sm) =>
            s.HallOpenTime = Parser.NullIfEmpty(v),
        ["OnlineHallCloseTime"] = (s, v, h, sm) =>
            s.HallCloseTime = Parser.NullIfEmpty(v),
        ["OnlineHallAvailabilityDayColors"] = (s, v, h, sm) =>
        {
            s.HallAvailabilityDayColors = v;
            if (string.IsNullOrEmpty(s.HallAvailabilityDayColors))
                s.HallAvailabilityDayColors = "1234";
        },
        ["OnlineHallAvailabilityWeekDays"] = (s, v, h, sm) =>
        {
            s.HallAvailabilityWeekDays = v;
            if (string.IsNullOrEmpty(s.HallAvailabilityWeekDays))
                s.HallAvailabilityWeekDays = "1234567";
        },
        ["OnlineHallBookingDeadline"] = (s, v, h, sm) =>
            s.OnlineHallBookingDeadline = Parser.NullIfEmpty(v),
        ["OnlineDefaultTimeForDinnerAndBowling"] = (s, v, h, sm) =>
            s.OnlineDefaultTimeForDinnerAndBowling = Parser.NullIfEmpty(v),
        ["OnlineCourtOpenTime"] = (s, v, h, sm) =>
            s.CourtOpenTime = Parser.NullIfEmpty(v),
        ["OnlineCourtCloseTime"] = (s, v, h, sm) =>
            s.CourtCloseTime = Parser.NullIfEmpty(v),
        ["OnlineCourtReservationBuffer"] = (s, v, h, sm) =>
            s.CourtReservationBuffer = v != "00:00" ? v : null,
        ["OnlineTableBookingReservationBufferStart"] = (s, v, h, sm) =>
            s.OnlineTableBookingReservationBufferStart = v,
        ["OnlineTableBookingReservationBufferEnd"] = (s, v, h, sm) =>
            s.OnlineTableBookingReservationBufferEnd = v,
        ["OnlineHallEmail"] = (s, v, h, sm) =>
            s.EmailHall = v,
        ["OnlineUrlLinkToNewsletter"] = (s, v, h, sm) =>
            s.UrlLinkToNewsletter = v,
        ["OnlineGoogleAnalytId"] = (s, v, h, sm) =>
            s.GoogleAnalyticId = Parser.NullIfEmpty(v),
        ["OnlineGoogleAnalytId2"] = (s, v, h, sm) =>
            s.GoogleAnalyticId2 = Parser.NullIfEmpty(v),
        ["OnlineImagesPath"] = (s, v, h, sm) =>
            s.Onlineimagepath = v,
        ["onlinegoogleconversionid"] = (s, v, h, sm) =>
            s.Googleconversionid = Parser.NullIfEmpty(v),
        ["onlinegoogleconversionlabel"] = (s, v, h, sm) =>
            s.Googleconversionlabel = Parser.NullIfEmpty(v),
        ["OnlineHallShowName"] = (s, v, h, sm) =>
            s.OnlineHallShowName = Parser.ToBool(v),
        ["OnlineHallUseVoucher"] = (s, v, h, sm) =>
            s.OnlineHallUseVoucher = Parser.ToBool(v),
        ["OnlineHallBookingDeadline"] = (s, v, h, sm) =>
            s.OnlineHallBookingDeadline = Parser.NullIfEmpty(v),
        ["OnlineDefaultTimeForDinnerAndBowling"] = (s, v, h, sm) =>
            s.OnlineDefaultTimeForDinnerAndBowling = Parser.NullIfEmpty(v),
        ["EniroDenmarkUsername"] = (s, v, h, sm) =>
            s.EniroDenmarkUsername = v,
        ["EniroDenmarkPassword"] = (s, v, h, sm) =>
            s.EniroDenmarkPassword = v,
        ["OnlineURL_GuestShopper"] = (s, v, h, sm) =>
            s.OnlineURL_GuestShopper = Parser.NullIfEmpty(v),
        ["OnlineURL_RoomBooking"] = (s, v, h, sm) =>
            s.OnlineURL_RoomBooking = Parser.NullIfEmpty(v),
        ["OnlineTerminalPersonalShopperURL"] = (s, v, h, sm) =>
            s.OnlineTerminalPersonalShopperURL = Parser.NullIfEmpty(v),
        ["OnlineTerminalOnline22URL"] = (s, v, h, sm) =>
            s.OnlineTerminalOnline22URL = Parser.NullIfEmpty(v),
        ["OnlineURL_CheckinNotPaid"] = (s, v, h, sm) =>
            s.OnlineURL_CheckinNotPaid = Parser.NullIfEmpty(v),
        ["OnlineZopimChatId"] = (s, v, h, sm) =>
            s.OnlineZopimChatId = Parser.NullIfEmpty(v),
        ["OnlineFacebookTrackingId"] = (s, v, h, sm) =>
            s.OnlineFacebookTrackingId = Parser.NullIfEmpty(v),
        ["OnlineHotelMailDomino"] = (s, v, h, sm) =>
            s.HotelMailDomino = Parser.NullIfEmpty(v),
        ["SalesChannelMail"] = (s, v, h, sm) =>
            s.SalesChannelMail = Parser.NullIfEmpty(v),
        ["onlinegoogletagmanagerid"] = (s, v, h, sm) =>
            s.Googletagmanagerid = Parser.NullIfEmpty(v),
        ["EmailServerIsOffice365"] = (s, v, h, sm) =>
            s.EmailServerIsOffice365 = Parser.ToBoolNotZero(v),
        ["email_host"] = (s, v, h, sm) =>
            s.EMail_Host = v,
        ["email_userid"] = (s, v, h, sm) =>
            s.EMail_UserID = v,
        ["email_password"] = (s, v, h, sm) =>
            s.EMail_Password = v,
        ["email_port"] = (s, v, h, sm) =>
            s.EMail_Port = Parser.ToInt(v),
        ["emailtls"] = (s, v, h, sm) =>
            s.EMailTLS = Parser.ToInt(v),
        ["emailenablestandardgui"] = (s, v, h, sm) =>
            s.EmailEnableStandardGUI = Parser.ToBool(v),
        ["emailenablestandardnongui"] = (s, v, h, sm) =>
            s.EmailEnableStandardNonGUI = Parser.ToBool(v),
        ["email_authentication"] = (s, v, h, sm) =>
            s.Email_authentication = Parser.ToBool(v),
        ["emailmsgraphapienablesendmailqueue"] = (s, v, h, sm) =>
            s.EmailMSGraphAPIEnableSendMailQueue = Parser.ToBool(v),
        ["emailmsgraphapimailenablegui"] = (s, v, h, sm) =>
            s.EmailMSGraphAPIMailEnableGUI = Parser.ToBool(v),
        ["emailmsgraphapiauthorizationflowgui"] = (s, v, h, sm) =>
            s.EmailMSGraphAPIAuthorizationFlowGUI = Parser.ToInt(v),
        ["emailmsgraphapitokenurlgui"] = (s, v, h, sm) =>
            s.EmailMSGraphAPITokenUrlGUI = v,
        ["emailmsgraphapiclientidgui"] = (s, v, h, sm) =>
            s.EmailMSGraphAPIClientIDGUI = v,
        ["emailmsgraphapiclientsecretgui"] = (s, v, h, sm) =>
            s.EmailMSGraphAPIClientSecretGUI = v,
        ["emailmsgraphapienablednongui"] = (s, v, h, sm) =>
            s.EmailMSGraphAPIEnabledNonGUI = Parser.ToBool(v),
        ["emailmsgraphapiauthorizationflownongui"] = (s, v, h, sm) =>
            s.EmailMSGraphAPIAuthorizationFlowNonGUI = Parser.ToInt(v),
        ["emailmsgraphapitokenurlnongui"] = (s, v, h, sm) =>
            s.EmailMSGraphAPITokenUrlNonGUI = v,
        ["emailmsgraphapiclientidnongui"] = (s, v, h, sm) =>
            s.EmailMSGraphAPIClientIDNonGUI = v,
        ["emailmsgraphapiclientsecretnongui"] = (s, v, h, sm) =>
            s.EmailMSGraphAPIClientSecretNonGUI = v,
        ["OnlineShowPricesForAllPax"] = (s, v, h, sm) =>
            s.OnlineShowPricesForAllPax = v.Contains("1"),
        ["OnlineShowLoginViaRoomNo"] = (s, v, h, sm) =>
            s.OnlineShowLoginViaRoomNo = Parser.ToInt(v),
        ["onlineusegoogleanalyticinheader"] = (s, v, h, sm) =>
            s.UseGoogleanalyticInHeader = Parser.ToBoolNotZero(v),
        ["onlinebookingunit"] = (s, v, h, sm) =>
            s.BookingUnit = Parser.ToInt(v),
        ["onlineisdemostander"] = (s, v, h, sm) =>
            s.IsDemoStander = Parser.ToBoolNotZero(v),
        ["onlineshowhoteldescpopup"] = (s, v, h, sm) =>
            s.ShowHoteldescriptionPopup = Parser.ToBoolNotZero(v),
        ["onlineshowproductdescpopup"] = (s, v, h, sm) =>
            s.ShowProductdescriptionPopup = Parser.ToBoolNotZero(v),
        ["onlineshowroomtypedescpopup"] = (s, v, h, sm) =>
            s.ShowRoomtypedescriptionPopup = Parser.ToBoolNotZero(v),
        ["onlineshowrelatedwareonextra"] = (s, v, h, sm) =>
            s.ShowRelatedwareOnExtra = Parser.ToBoolNotZero(v),
        ["onlineshowlargepicturepopup"] = (s, v, h, sm) =>
            s.ShowLargePicturePopup = Parser.ToBoolNotZero(v),
        ["onlineusecreditcardfee"] = (s, v, h, sm) =>
            s.UseCreditcardFee = Parser.ToBoolNotZero(v),
        ["onlineterminalpaymentbeforecheckin"] = (s, v, h, sm) =>
            s.Terminalpaymentbeforecheckin = Parser.ToBoolNotZero(v),
        ["onlinezopimchatid"] = (s, v, h, sm) =>
            s.OnlineZopimChatId = Parser.NullIfEmpty(v),
        ["OnlineFacebookTrackingId"] = (s, v, h, sm) =>
            s.OnlineFacebookTrackingId = Parser.NullIfEmpty(v),
        ["onlineuseonlinegold"] = (s, v, h, sm) =>
            s.UseOnlineGold = Parser.ToBoolNotZero(v),
        ["OnlineHotelMailDomino"] = (s, v, h, sm) =>
            s.HotelMailDomino = Parser.NullIfEmpty(v),
        ["onlineusecreditcardfee"] = (s, v, h, sm) =>
            s.UseCreditcardFee = Parser.ToBoolNotZero(v),
        ["onlineusearrivaltime"] = (s, v, h, sm) =>
            s.UseArrivalTime = Parser.ToBoolNotZero(v),
        ["onlinearrivaltimemaxvalue"] = (s, v, h, sm) =>
            s.ArrivalTimeMaxValue = Parser.ToInt(v),
        ["onlinemintotalstaydays"] = (s, v, h, sm) =>
            s.MinTotalStayDays = Parser.ToInt(v),
        ["onlinegoogletagmanagerid"] = (s, v, h, sm) =>
            s.Googletagmanagerid = Parser.NullIfEmpty(v),
        ["onlineusegoogletagmanager"] = (s, v, h, sm) =>
            s.UseGooglTagManager = Parser.ToBoolNotZero(v),
        ["onlineusecampaigncoderoom"] = (s, v, h, sm) =>
            s.UseCampaignCodeRoom = Parser.ToBoolNotZero(v),
        ["onlineusecustomerselectcurrency"] = (s, v, h, sm) =>
            s.UseCustomerSelectCurrency = Parser.ToBoolNotZero(v),
        ["onlineusevisualcurrency"] = (s, v, h, sm) =>
            s.UseVisualCurrency = Parser.ToBoolNotZero(v),
        ["onlinebookwithzeropriceperdayfromproduct"] = (s, v, h, sm) =>
            s.Bookwithzeropriceperdayfromproduct = Parser.ToBoolNotZero(v),
        ["onlineavailabilitysorttype"] = (s, v, h, sm) =>
            s.SortAvailabilityType = Parser.ToInt(v),
        ["onlineusesegmentposfromproduct"] = (s, v, h, sm) =>
            s.UseSegmentPosFromProduct = Parser.ToBoolNotZero(v),
        ["HallClimbingUseClimbingAvailability"] = (s, v, h, sm) =>
            s.HallClimbingUseClimbingAvailability = Parser.ToBoolNotZero(v),
        ["onlineusecookiepopuptest"] = (s, v, h, sm) =>
            s.UseCookiePopupTest = Parser.ToBoolNotZero(v),
        ["onlineusepdfmail"] = (s, v, h, sm) =>
            s.UseConfirmpdfMail = Parser.ToBoolNotZero(v),
        ["onlineusewihpscript"] = (s, v, h, sm) =>
            s.WihpId = Parser.NullIfEmpty(v),
        ["onlineterminalloginmode"] = (s, v, h, sm) =>
        {
            switch (Parser.ToInt(v))
            {
                case 5: s.TerminalLoginMode = TerminalLoginModes.ResNumberAndResIdCode; break;
                case 4: s.TerminalLoginMode = TerminalLoginModes.ResNumberAndSurname; break;
                case 3: s.TerminalLoginMode = TerminalLoginModes.ZipCodeResNumberAndZipCodeEmail; break;
                case 2: s.TerminalLoginMode = TerminalLoginModes.ResNumberAndZipCodeEmail; break;
                case 6: s.TerminalLoginMode = TerminalLoginModes.CompanyAndGuestName; break;
                case 7: s.TerminalLoginMode = TerminalLoginModes.ResNoOrPhoneOrName; break;
                case 8: s.TerminalLoginMode = TerminalLoginModes.NameOrPhoneOrResNo; break;
                default: s.TerminalLoginMode = TerminalLoginModes.ResNumberOnly; break;
            }
        },
        ["OnlineHallsProductWareByPOS"] = (s, v, h, sm) =>
        {
            switch (Parser.ToInt(v))
            {
                case 0: s.HallsProductWareByPOS = productWareByPOSType.No; break;
                case 1: s.HallsProductWareByPOS = productWareByPOSType.Company; break;
                case 2: s.HallsProductWareByPOS = productWareByPOSType.ContactPerson; break;
            }
        },
        ["OnlineRoomsProductWareByPOS"] = (s, v, h, sm) =>
            s.RoomsProductWareByPOS = Parser.ToBoolNotZero(v),
        ["onlineusewarewithrules"] = (s, v, h, sm) =>
            s.Usewarewithrules = Parser.ToBoolNotZero(v),
        ["onlineshowcurrencybeforeprice"] = (s, v, h, sm) =>
            s.ShowCurrenyBeforePrice = Parser.ToBoolNotZero(v),
        ["onlineusewarewithrulesonlytime"] = (s, v, h, sm) =>
            s.UsewarewithrulesOnlyTime = Parser.ToBoolNotZero(v),
        ["onlineuseuniversalanalytic"] = (s, v, h, sm) =>
            s.UseUniversalAnalytic = Parser.ToBoolNotZero(v),
        ["OnlineSplitPaymentOnCreditCard"] = (s, v, h, sm) =>
            s.UsePaymentOnDifferentAccounts = Parser.ToBoolNotZero(v),
        ["onlinebookonlypersons"] = (s, v, h, sm) =>
            s.BookOnlyPersons = Parser.ToBoolNotZero(v),
        ["onlineuseuservoice"] = (s, v, h, sm) =>
            s.UseUserVoiceScript = Parser.ToBoolNotZero(v),
        ["onlineusepicassoonlineservice"] = (s, v, h, sm) =>
            s.UsePicassoOnlineService = Parser.ToBoolNotZero(v),
        ["OnlineRoomTypeTextPerPAX"] = (s, v, h, sm) =>
            s.UsedynamicPaxText = Parser.ToBoolNotZero(v),
        ["onlinefirstlastname2lines"] = (s, v, h, sm) =>
            s.ShowFirstLastname2Lines = Parser.ToBoolNotZero(v),
        ["onlinegoldlargeproductpicture"] = (s, v, h, sm) =>
            s.UseGoldlargeproductpicture = Parser.ToBoolNotZero(v),
        ["onlinegolddominotopv2"] = (s, v, h, sm) =>
            s.UseGolddominotopv2 = Parser.ToBoolNotZero(v),
        ["onlineaddconfirmtojournal"] = (s, v, h, sm) =>
            s.AddConfirmLetterToJournal = Parser.ToBoolNotZero(v),
        ["onlinestayloggedinafterconfirm"] = (s, v, h, sm) =>
            s.CustomerStayLoginAfterConfirm = Parser.ToBoolNotZero(v),
        ["onlineusepatienthotelgold"] = (s, v, h, sm) =>
            s.UsePatientBookingGold = Parser.ToBoolNotZero(v),
        ["onlineusecalendaravailability"] = (s, v, h, sm) =>
            s.UseCalendarAvailability = Parser.ToBoolNotZero(v),
        ["onlineshowactivitycodeinmail"] = (s, v, h, sm) =>
            s.ShowActivityNoteOnConfirmMail = Parser.ToBoolNotZero(v),
        ["onlineusecustomerlogindominotopv2"] = (s, v, h, sm) =>
            s.UseCustomerLoginWithDominoTopV2 = Parser.ToBoolNotZero(v),
        ["onlineusedominoavailability"] = (s, v, h, sm) =>
            s.UseDominoAvailability = Parser.ToBoolNotZero(v),
        ["onlineusedominoflowcache"] = (s, v, h, sm) =>
            s.UseDominoFlowCache = Parser.ToBoolNotZero(v),

        // =============================================
        // INTEGER SETTINGS
        // =============================================
        ["OnlineConfirmmailTemplate"] = (s, v, h, sm) =>
            s.ConfirmmailTemplate = Parser.ToInt(v),
        ["NumberOfDecimalsInPrices"] = (s, v, h, sm) =>
            s.NumberOfDecimalsInPrice = Parser.ToInt(v),
        ["HallBookingInterval"] = (s, v, h, sm) =>
            s.HallBookingInterval = Parser.ToInt(v),
        ["OnlineSkipPageNumber"] = (s, v, h, sm) =>
        {
            try { s.GoToPageAfterSearch = Parser.ToInt(v, 1); }
            catch (Exception e) { sm.SendErrorMail($"Hotelid: {h}<br>Error at OnlineSkipPageNumber.<br>{e.Message}", h); }
        },
        ["OnlineCustomerLoginMode"] = (s, v, h, sm) =>
        {
            try { s.Loginmode = Parser.ToInt(v); }
            catch (Exception e) { sm.SendErrorMail($"Hotelid: {h}<br>Error at OnlineCustomerLoginMode.<br>{e.Message}", h); }
        },
        ["OnlineCustomerRateMode"] = (s, v, h, sm) =>
        {
            try { s.CustomerRateMode = Parser.ToInt(v); }
            catch (Exception e) { sm.SendErrorMail($"Hotelid: {h}<br>Error at OnlineCustomerRateMode.<br>{e.Message}", h); }
        },
        ["OnlineCourtNumberOfAllowedReservationsPerDay"] = (s, v, h, sm) =>
        {
            try { s.CourtNumberOfAllowedReservationsPerDay = Parser.ToInt(v); }
            catch (Exception e) { sm.SendErrorMail($"Hotelid: {h}<br>Error at OnlineCourtNumberOfAllowedReservationsPerDay.<br>{e.Message}", h); }
        },
        ["monanocheckinrestypes"] = (s, v, h, sm) =>
            s.MonaNoCheckinResTypes = Parser.ToInt(v),
        ["OnlineHallDisplayCategory"] = (s, v, h, sm) =>
            s.OnlineHallDisplayCategory = Parser.ToInt(v),
        ["OnlineIntervalBetweenArrivals"] = (s, v, h, sm) =>
            s.OnlineIntervalBetweenArrivals = Parser.ToInt(v),
        ["OnlineMaxReservationArrivals"] = (s, v, h, sm) =>
            s.OnlineMaxReservationArrivals = Parser.ToInt(v),
        ["OnlineMaxPaxOnArrival"] = (s, v, h, sm) =>
            s.OnlineMaxPaxOnArrival = Parser.ToInt(v),
        ["OnlineMaxPaxOnReservation"] = (s, v, h, sm) =>
            s.OnlineMaxPaxOnReservation = Parser.ToInt(v),
        ["OnlineMaxReservationOnArrival"] = (s, v, h, sm) =>
            s.OnlineMaxReservationOnArrival = Parser.ToInt(v),
        ["OnlineMaxMinutesAfterScheduleClose"] = (s, v, h, sm) =>
            s.OnlineMaxMinutesAfterScheduleClose = Parser.ToInt(v),
        ["GiftCertificateMonth"] = (s, v, h, sm) =>
            s.OnlineGiftTokenExpireMonths = Parser.ToInt(v),
        ["OnlineShowLoginViaRoomNo"] = (s, v, h, sm) =>
            s.OnlineShowLoginViaRoomNo = Parser.ToInt(v),
        ["onlineusegoogleanalyticinheader"] = (s, v, h, sm) =>
            s.UseGoogleanalyticInHeader = Parser.ToBoolNotZero(v),
        ["onlinebookingunit"] = (s, v, h, sm) =>
            s.BookingUnit = Parser.ToInt(v),
        ["onlineisdemostander"] = (s, v, h, sm) =>
            s.IsDemoStander = Parser.ToBoolNotZero(v),
        ["onlineshowhoteldescpopup"] = (s, v, h, sm) =>
            s.ShowHoteldescriptionPopup = Parser.ToBoolNotZero(v),
        ["onlineshowproductdescpopup"] = (s, v, h, sm) =>
            s.ShowProductdescriptionPopup = Parser.ToBoolNotZero(v),
        ["onlineshowroomtypedescpopup"] = (s, v, h, sm) =>
            s.ShowRoomtypedescriptionPopup = Parser.ToBoolNotZero(v),
        ["onlineshowrelatedwareonextra"] = (s, v, h, sm) =>
            s.ShowRelatedwareOnExtra = Parser.ToBoolNotZero(v),
        ["onlineshowlargepicturepopup"] = (s, v, h, sm) =>
            s.ShowLargePicturePopup = Parser.ToBoolNotZero(v),
        ["onlineusecreditcardfee"] = (s, v, h, sm) =>
            s.UseCreditcardFee = Parser.ToBoolNotZero(v),
        ["onlineterminalpaymentbeforecheckin"] = (s, v, h, sm) =>
            s.Terminalpaymentbeforecheckin = Parser.ToBoolNotZero(v),
        ["onlinezopimchatid"] = (s, v, h, sm) =>
            s.OnlineZopimChatId = Parser.NullIfEmpty(v),
        ["OnlineFacebookTrackingId"] = (s, v, h, sm) =>
            s.OnlineFacebookTrackingId = Parser.NullIfEmpty(v),
        ["onlineuseonlinegold"] = (s, v, h, sm) =>
            s.UseOnlineGold = Parser.ToBoolNotZero(v),
        ["OnlineHotelMailDomino"] = (s, v, h, sm) =>
            s.HotelMailDomino = Parser.NullIfEmpty(v),
        ["onlineusearrivaltime"] = (s, v, h, sm) =>
            s.UseArrivalTime = Parser.ToBoolNotZero(v),
        ["onlinearrivaltimemaxvalue"] = (s, v, h, sm) =>
            s.ArrivalTimeMaxValue = Parser.ToInt(v),
        ["onlinemintotalstaydays"] = (s, v, h, sm) =>
            s.MinTotalStayDays = Parser.ToInt(v),
        ["SalesChannelMail"] = (s, v, h, sm) =>
            s.SalesChannelMail = Parser.NullIfEmpty(v),
        ["onlineusegoogletagmanager"] = (s, v, h, sm) =>
            s.UseGooglTagManager = Parser.ToBoolNotZero(v),
        ["onlineusecampaigncoderoom"] = (s, v, h, sm) =>
            s.UseCampaignCodeRoom = Parser.ToBoolNotZero(v),
        ["onlineusecustomerselectcurrency"] = (s, v, h, sm) =>
            s.UseCustomerSelectCurrency = Parser.ToBoolNotZero(v),
        ["onlineusevisualcurrency"] = (s, v, h, sm) =>
            s.UseVisualCurrency = Parser.ToBoolNotZero(v),
        ["onlinebookwithzeropriceperdayfromproduct"] = (s, v, h, sm) =>
            s.Bookwithzeropriceperdayfromproduct = Parser.ToBoolNotZero(v),
        ["onlineavailabilitysorttype"] = (s, v, h, sm) =>
            s.SortAvailabilityType = Parser.ToInt(v),
        ["onlineusesegmentposfromproduct"] = (s, v, h, sm) =>
            s.UseSegmentPosFromProduct = Parser.ToBoolNotZero(v),
        ["HallClimbingUseClimbingAvailability"] = (s, v, h, sm) =>
            s.HallClimbingUseClimbingAvailability = Parser.ToBoolNotZero(v),
        ["onlineusecookiepopuptest"] = (s, v, h, sm) =>
            s.UseCookiePopupTest = Parser.ToBoolNotZero(v),
        ["onlineusepdfmail"] = (s, v, h, sm) =>
            s.UseConfirmpdfMail = Parser.ToBoolNotZero(v),
        ["onlineusewihpscript"] = (s, v, h, sm) =>
            s.WihpId = Parser.NullIfEmpty(v),
        ["onlineterminalloginmode"] = (s, v, h, sm) =>
        {
            switch (Parser.ToInt(v))
            {
                case 5: s.TerminalLoginMode = TerminalLoginModes.ResNumberAndResIdCode; break;
                case 4: s.TerminalLoginMode = TerminalLoginModes.ResNumberAndSurname; break;
                case 3: s.TerminalLoginMode = TerminalLoginModes.ZipCodeResNumberAndZipCodeEmail; break;
                case 2: s.TerminalLoginMode = TerminalLoginModes.ResNumberAndZipCodeEmail; break;
                case 6: s.TerminalLoginMode = TerminalLoginModes.CompanyAndGuestName; break;
                case 7: s.TerminalLoginMode = TerminalLoginModes.ResNoOrPhoneOrName; break;
                case 8: s.TerminalLoginMode = TerminalLoginModes.NameOrPhoneOrResNo; break;
                default: s.TerminalLoginMode = TerminalLoginModes.ResNumberOnly; break;
            }
        },
        ["OnlineHallsProductWareByPOS"] = (s, v, h, sm) =>
        {
            switch (Parser.ToInt(v))
            {
                case 0: s.HallsProductWareByPOS = productWareByPOSType.No; break;
                case 1: s.HallsProductWareByPOS = productWareByPOSType.Company; break;
                case 2: s.HallsProductWareByPOS = productWareByPOSType.ContactPerson; break;
            }
        },
        ["OnlineRoomsProductWareByPOS"] = (s, v, h, sm) =>
            s.RoomsProductWareByPOS = Parser.ToBoolNotZero(v),
        ["onlineusewarewithrules"] = (s, v, h, sm) =>
            s.Usewarewithrules = Parser.ToBoolNotZero(v),
        ["onlineshowcurrencybeforeprice"] = (s, v, h, sm) =>
            s.ShowCurrenyBeforePrice = Parser.ToBoolNotZero(v),
        ["onlineusewarewithrulesonlytime"] = (s, v, h, sm) =>
            s.UsewarewithrulesOnlyTime = Parser.ToBoolNotZero(v),
        ["onlineuseuniversalanalytic"] = (s, v, h, sm) =>
            s.UseUniversalAnalytic = Parser.ToBoolNotZero(v),
        ["OnlineSplitPaymentOnCreditCard"] = (s, v, h, sm) =>
            s.UsePaymentOnDifferentAccounts = Parser.ToBoolNotZero(v),
        ["onlinebookonlypersons"] = (s, v, h, sm) =>
            s.BookOnlyPersons = Parser.ToBoolNotZero(v),
        ["onlineuseuservoice"] = (s, v, h, sm) =>
            s.UseUserVoiceScript = Parser.ToBoolNotZero(v),
        ["onlineusepicassoonlineservice"] = (s, v, h, sm) =>
            s.UsePicassoOnlineService = Parser.ToBoolNotZero(v),
        ["OnlineRoomTypeTextPerPAX"] = (s, v, h, sm) =>
            s.UsedynamicPaxText = Parser.ToBoolNotZero(v),
        ["onlinefirstlastname2lines"] = (s, v, h, sm) =>
            s.ShowFirstLastname2Lines = Parser.ToBoolNotZero(v),
        ["onlinegoldlargeproductpicture"] = (s, v, h, sm) =>
            s.UseGoldlargeproductpicture = Parser.ToBoolNotZero(v),
        ["onlinegolddominotopv2"] = (s, v, h, sm) =>
            s.UseGolddominotopv2 = Parser.ToBoolNotZero(v),
        ["onlineaddconfirmtojournal"] = (s, v, h, sm) =>
            s.AddConfirmLetterToJournal = Parser.ToBoolNotZero(v),
        ["onlinestayloggedinafterconfirm"] = (s, v, h, sm) =>
            s.CustomerStayLoginAfterConfirm = Parser.ToBoolNotZero(v),
        ["PicassoOnlineSegmentNumber"] = (s, v, h, sm) =>
            s.PicassoOnlineSegmentNumber = Parser.ToInt(v),
        ["OnlineRoomnumberBookResource"] = (s, v, h, sm) =>
            s.RoomnumberBookReseorceNumber = Parser.ToInt(v),
        ["OnlineRoomtypeBookResource"] = (s, v, h, sm) =>
            s.RoomtypeBookResourcenumber = Parser.ToInt(v),
        ["OnlineDaysBeforeDinnerAndBowling"] = (s, v, h, sm) =>
            s.OnlineDaysBeforeDinnerAndBowling = Parser.ToInt(v),
        ["OnlineTerminalMaxGroupCount"] = (s, v, h, sm) =>
            s.TerminalMaxGroupCount = Parser.ToInt(v),
        ["OnlineMaxPaxOnArrival"] = (s, v, h, sm) =>
            s.OnlineMaxPaxOnArrival = Parser.ToInt(v),
        ["OnlineMaxPaxOnReservation"] = (s, v, h, sm) =>
            s.OnlineMaxPaxOnReservation = Parser.ToInt(v),
        ["OnlineMaxReservationOnArrival"] = (s, v, h, sm) =>
            s.OnlineMaxReservationOnArrival = Parser.ToInt(v),
        ["OnlineMaxMinutesAfterScheduleClose"] = (s, v, h, sm) =>
            s.OnlineMaxMinutesAfterScheduleClose = Parser.ToInt(v),
        ["GiftCertificateMonth"] = (s, v, h, sm) =>
            s.OnlineGiftTokenExpireMonths = Parser.ToInt(v),
        ["OnlineSearchMaxRooms"] = (s, v, h, sm) =>
            s.SearchMaxRooms = Parser.ToInt(v),
        ["DefaultTableBookingMinutes"] = (s, v, h, sm) =>
            s.DefaultTableBookingMinutes = Parser.ToInt(v),
        ["OnlineTerminalMakeKeyCopyIsWrongWithin"] = (s, v, h, sm) =>
            s.TerminalMakeKeyCopyIsWrongWithin = Parser.ToInt(v),
        ["earliestcheckintime"] = (s, v, h, sm) =>
            s.EarliestCheckinTime = Parser.NullIfEmpty(v),
        ["checkearliestcheckintimeagainstarrivaltime"] = (s, v, h, sm) =>
            s.CheckEarliestCheckinTimeAgainstArrivalTime = Parser.ToBool(v),
        ["cleaningmainsubinteraction"] = (s, v, h, sm) =>
            s.CleaningMainSubInteraction = Parser.ToBoolNotZero(v),
        ["checkcleanstatusatcheckin"] = (s, v, h, sm) =>
            s.CheckCleanStatusAtCheckin = Parser.ToBoolNotZero(v),
        ["meetbookingsendletterfrom"] = (s, v, h, sm) =>
            s.MeetBookingSendLetterFrom = Parser.ToInt(v),
        ["EnableDigitalCheckInOut"] = (s, v, h, sm) =>
            s.EnableDigitalCheckInOut = Parser.ToBool(v),
        ["onlineguestsplitbill"] = (s, v, h, sm) =>
            s.Guestsplitbill = Parser.ToInt(v),
        ["onlinecompanysplitbill"] = (s, v, h, sm) =>
            s.Companysplitbill = Parser.ToInt(v),
        ["onlineagencysplitbill"] = (s, v, h, sm) =>
            s.Agencysplitbill = Parser.ToInt(v),
        ["OnlineCopyConfirmations"] = (s, v, h, sm) =>
            s.OnlineCopyConfirmationsFromPicasso = Parser.ToBool(v),
        ["IsDanHostel"] = (s, v, h, sm) =>
            s.IsDanHostel = Parser.ToBoolNotZero(v),
        ["OnlineTerminalDetailedInterfaceResponse"] = (s, v, h, sm) =>
            s.IsTerminalDetailedInterface = Parser.ToBool(v),
        ["OnlineCheckoutDontPostDeposit"] = (s, v, h, sm) =>
            s.CheckoutDontPostDeposit = Parser.ToBool(v),
        ["onlineemptycartonotherhotelsearch"] = (s, v, h, sm) =>
            s.Emptycartonotherhotelsearch = Parser.ToBool(v),
        ["onlineusepictureonextra"] = (s, v, h, sm) =>
            s.UsePictureOnExtra = Parser.ToBool(v),
        ["onlineallowusertochooseroomnumber"] = (s, v, h, sm) =>
            s.AllowUserToChooseRoomNumber = Parser.ToBool(v),
        ["onlineusecurrencybycountrycode"] = (s, v, h, sm) =>
            s.UseCurrencyByCountryCode = Parser.ToBool(v),
        ["onlineusedanhostelmembercard"] = (s, v, h, sm) =>
            s.UseDanhostelMemberCard = Parser.ToBool(v),
        ["onlineuselargepax"] = (s, v, h, sm) =>
            s.UseLargePax = Parser.ToBool(v),
        ["onlineuseexternallink"] = (s, v, h, sm) =>
            s.UseExternalLink = Parser.ToBool(v),
        ["OnlineShowPricesForAllPax"] = (s, v, h, sm) =>
            s.OnlineShowPricesForAllPax = v.Contains("1"),
        ["OnlineLoginWithPwdParm"] = (s, v, h, sm) =>
            s.LoginWithPwdParm = Parser.ToBool(v),

        // =============================================
        // SPECIAL CASES
        // =============================================
        ["OnlineHallShowTableSetup"] = (s, v, h, sm) =>
        {
            s.HallShowTableSetup = Parser.ToBoolNotZero(v);
            s.HallShowTableSetupOnSearchForm = Parser.ToBool(v);
        },
        ["OnlinePatientHotelVersion"] = (s, v, h, sm) =>
        {
            var patientsetup = new PatientHotelSetup(Parser.ToInt(v));
            s.PatientHotelSetup = patientsetup;
        },
        ["onlineavailabilityratelevel"] = (s, v, h, sm) =>
        {
            if (!string.IsNullOrEmpty(v))
            {
                switch (Parser.ToInt(v))
                {
                    case 1:
                    case 2:
                        s.UseAvailabilityRateLevel = true;
                        break;
                    default:
                        s.UseAvailabilityRateLevel = false;
                        break;
                }
            }
            else
            {
                s.UseAvailabilityRateLevel = false;
            }
        },
        ["KeySystem"] = (s, v, h, sm) =>
            s.TerminalKeySystem = Parser.ToEnum(v, KeySystem.Unknown),
        ["OnlineTerminalPaymentMode"] = (s, v, h, sm) =>
            s.OnlineTerminalPaymentMode = Parser.ToEnum(v, TerminalPaymentModes.ResNumberOnly),
    };

    // =============================================
    // MAIN LOAD METHOD
    // =============================================
    public static void LoadHotelSetupSearch(SqlDataReader sqlReader, HotelSetupSearch hotelSetupSearch, 
        int hotelId, SendMailService sm)
    {
        while (sqlReader.Read())
        {
            string keyvalue = sqlReader["Keyvalue"] + "";
            string searchvalue = sqlReader["searchkey"] + "";

            if (_settingMappings.TryGetValue(searchvalue, out var mapping))
            {
                try
                {
                    mapping(hotelSetupSearch, keyvalue, hotelId, sm);
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other settings
                    sm?.SendErrorMail(
                        $
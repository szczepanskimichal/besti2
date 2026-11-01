// BusinessListItemDTO type definition!!!
export type BusinessListItem = {
    id: string;
    slug: string; // page address: bestiapp.no/{slug}
    name: string;
    category: string;
    description: string | null;
    city: string;
    logoUrl: string | null;
    freeSlots: FreeSlot[] | null; // only when searching with a date
    distanceKm: number | null; // only with "Nær meg"
    averageRating: number | null; // e.g. 4.7; null when nobody has reviewed yet
    reviewCount: number;
    isVerified: boolean; // "Verifisert bedrift": active in Brønnøysund and approved by BESTI
};

export type FreeSlot = {
    startUtc: string;
    serviceId: string;
    serviceName: string;
    employeeId: string;
};
// One day in the "Velg dag" strip; employeeId = who is free at that time
// "År" in the panel calendar: visits per day (days without visits are left out)
export type BookingDayCount = {
    date: string;
    count: number;
};

export type FreeDay = {
    date: string;
    slots: { startUtc: string; employeeId: string }[];
};

export type Service = {
    id: string;
    name: string;
    description: string | null;
    durationMinutes: number;
    priceNok: number | null;
    priceType: string;
};

export type Employee = {
    id: string;
    name: string;
    lastName: string;
};


export type OpeningHours = {
    dayOfWeek: number;
    opensAt: string;
    closesAt: string;
    breakStart?: string | null; // a daily break nobody can book (lunch); both null = no break
    breakEnd?: string | null;
};

export type BusinessDetails = {
    id: string;
    slug: string; // page address: bestiapp.no/{slug}
    name: string;
    category: string;
    description: string | null;
    // Public contact e-mail; null = only the phone is shown
    email: string | null;
    phoneNumber: string;
    address: string;
    postalCode: string;
    city: string;
    services: Service[];
    employees: Employee[];
    openingHours: OpeningHours[];
    latitude: number | null;
    longitude: number | null;
    logoUrl: string | null;
    gallery: GalleryImage[];
    // Only true in the owner's panel: waiting for BESTI's approval, customers cannot see it yet
    isHidden: boolean;
    freeCancelHours: number; // free cancellation until X hours before the visit; 0 = any time
    website: string | null; // always starts with http:// or https://
    autoConfirm: boolean; // new online bookings are confirmed at once
    // "Verifisert bedrift": when Brønnøysund last said the business is active; null = no badge
    verifiedUtc: string | null;
    loyaltyVisits: number | null; // "Stempelkort": every N-th visit gives loyaltyReward; null = none
    loyaltyReward: string | null;
    suspendedReason?: string | null; // only in the panel: BESTI has closed the page, and why
    joinedUtc: string | null; // when the business went live on BESTI ("Ny på Besti" the first week)
};

// "Forslag for din bransje" in the panel: a ready service the owner can add with one click
export type ServiceTemplate = {
    name: string;
    description: string | null;
    durationMinutes: number;
    priceNok: number | null;
    priceType: string;
};

export type BusinessApproval = {
    name: string;
    orgNumber: string;
    category: string;
    address: string;
    postalCode: string;
    city: string;
    email: string;
    phoneNumber: string;
};

export type GalleryImage = {
    id: string;
    url: string;
};

export type CreateBookingInput = {
    serviceId: string;
    employeeId: string;
    startUtc: string;
    customerName: string;
    customerPhone: string;
    customerEmail: string;
    marketingConsent: boolean;
    note?: string;
    units?: number; // "Per time" services: how many hours in a row
};

export type PanelBooking = {
    id: string;
    startUtc: string;
    endUtc: string;
    status: string;
    serviceName: string;
    employeeName: string;
    customerName: string;
    customerPhone: string;
    customerEmail: string;
    customerNote: string | null;
    employeeId: string; // the calendar puts the booking in this employee's column
    previousStartUtc: string | null; // set while a moved visit waits for the customer ("VenterKunde")
    noShowCount: number; // "Møtte ikke" for this customer at this business
    inviteId: string | null; // BESTI sammen: all visits of one group share it (👯)
    group: PanelGroup | null;
    customerKey: string | null; // "Kunder": the customer's card (null = anonymized)
    salonNote: string | null; // the business's own note about this customer
};

// BESTI sammen seen by the business: places, how many said yes, until when; open = more may still join
export type PanelGroup = {
    id: string;
    persons: number;
    joined: number;
    expiresUtc: string;
    open: boolean;
    organizerName: string;
    organizerComes: boolean;
    organizerPays: boolean; // "Jeg spanderer": the organizer pays for the whole group
    isOrganizer: boolean; // this booking is the organizer's own visit
    names: string[]; // everyone booked, in the order they said yes
};
// Blocked time in the calendar; employeeId null = the whole business is closed
export type TimeOff = {
    id: string;
    employeeId: string | null;
    employeeName: string | null;
    startUtc: string;
    endUtc: string;
    reason: string | null;
};

export type CustomerBooking = {
    id: string;
    startUtc: string;
    endUtc: string;
    status: string;
    serviceName: string;
    employeeName: string;
    businessId: string;
    businessName: string;
    businessSlug: string; // link to the business page: /{businessSlug}
    businessAddress: string;
    serviceId: string;
    employeeId: string;
    isReviewed: boolean; // the customer has already reviewed this visit
    businessPhone: string;
    freeCancelHours: number;
    previousStartUtc: string | null; // set while the business waits for the customer to accept a new time
    inviteId: string | null; // BESTI sammen: booked together with friends
};

// BESTI sammen: the invitation behind /invitasjon/{token}. Only the inviter's first name, never contact details
export type InviteStatus = "Venter" | "Akseptert" | "Avslatt" | "Utlopt" | "Trukket";

export type PayMode = "HverSelv" | "JegSpanderer";

export type Invite = {
    id: string;
    status: InviteStatus;
    inviterFirstName: string;
    names: string[]; // first names of everyone booked, the inviter first (when they come)
    businessName: string;
    businessSlug: string;
    businessAddress: string;
    serviceName: string;
    visitMinutes: number;
    priceNok: number | null;
    priceType: string;
    startUtc: string;
    expiresUtc: string;
    payMode: PayMode;
    persons: number; // all visits in the group
    organizerJoins: boolean; // false = the inviter only sends (and maybe pays), without a visit
    joined: number; // booked so far
    placesLeft: number; // places still free for others
    open: boolean; // new people can still join
    isInviter: boolean; // the logged-in visitor sent it
    isMember: boolean; // the logged-in visitor is booked in it
    stillFree: boolean; // a place for the visitor is still free right now
    // Short notes the friends wrote to the inviter with their answer ("Takk for at du tenkte på meg!"); only the inviter gets them
    friendMessages?: { name: string; text: string; icon: ReactionKind | null; joined: boolean }[] | null;
    // BESTI-chat: the inviter and the friends who joined (mine = written by the logged-in visitor,
    // canDelete = mine, or the visitor is the inviter); null = the visitor may not read it
    chat?: { id: string; name: string; text: string; mine: boolean; canDelete: boolean; time: string; reactions: ChatReaction[] }[] | null;
};

// Crystal reactions on a BESTI-chat message, in the house style instead of emoji
export type ReactionKind = "hjerte" | "tommel" | "latter" | "trist" | "stjerne" | "feiring";
export type ChatReaction = { kind: ReactionKind; names: string[] }; // first names, the visitor included as "Du"

// One invitation in Min Besti
export type CustomerInvite = {
    id: string;
    status: InviteStatus;
    isInviter: boolean;
    inviterFirstName: string;
    names: string[]; // everyone booked, the inviter first
    businessName: string;
    businessSlug: string;
    serviceName: string;
    startUtc: string;
    expiresUtc: string;
    payMode: PayMode;
    persons: number;
    joined: number;
    open: boolean; // more people can still join
};

export type CreateInviteInput = {
    serviceId: string;
    employeeId: string | null; // null = Hvem som helst
    startUtc: string;
    phone: string;
    payMode: PayMode;
    decisionMinutes: number;
    note: string;
    units: number;
    persons: number; // 2-8, the inviter included when organizerJoins
    organizerJoins: boolean;
};

export type Review = {
    id: string;
    stars: number;
    comment: string | null;
    customerName: string;
    createdUtc: string;
    businessReply: string | null;
    photoUrl: string | null; // the customer's photo from the visit
    helpful: number; // how many found it "Nyttig"
    markedHelpful: boolean; // the logged-in reader did
    hidden: boolean; // hidden by BESTI (only in the panel)
    hiddenReason: string | null;
};

export type BusinessReviews = {
    average: number | null; // null when there are no reviews yet
    count: number;
    reviews: Review[];
};

// One page of the business list (24 per page)
export type BusinessPage = {
    items: BusinessListItem[];
    page: number;
    pageCount: number;
    totalCount: number;
};
// "Ny tid" page: the moved visit behind the link in the e-mail
export type NewTime = {
    businessName: string;
    serviceName: string;
    employeeName: string;
    startUtc: string;
    previousStartUtc: string | null;
    status: string;
};
// The bell (🔔): newest notifications and how many are unread
export type NotificationItem = {
    id: string;
    text: string;
    link: string | null;
    createdUtc: string;
    isRead: boolean;
};

export type NotificationList = {
    unread: number;
    items: NotificationItem[];
};

// "Si fra hvis det blir ledig": one day a customer waits for (notified = the message has been sent)
export type WaitlistEntry = {
    id: string;
    businessName: string;
    businessSlug: string;
    serviceId: string;
    serviceName: string;
    employeeId: string | null;
    employeeName: string | null;
    date: string; // "2026-10-30" in Oslo
    notified: boolean;
};

// "Tid for ny time?": the customer is reminded to book this service again N weeks after each visit
export type RebookReminder = {
    id: string;
    businessName: string;
    businessSlug: string;
    serviceId: string;
    serviceName: string;
    employeeId: string | null;
    weeks: number;
};

// "Statistikk" in the panel (revenue is estimated from the prices saved on the bookings)
export type StatsNumbers = {
    visits: number;
    revenue: number;
    visitsWithoutPrice: number;
    cancelled: number;
    noShows: number;
    newCustomers: number;
    returningCustomers: number;
    online: number;
    manual: number;
};

export type StatsRow = { name: string; visits: number; revenue: number };

export type StatsTip = { kind: "tips" | "pro"; title: string; text: string; link: string | null };

export type BusinessStats = {
    days: number;
    current: StatsNumbers;
    previous: StatsNumbers;
    upcomingVisits: number;
    upcomingRevenue: number;
    averageStars: number | null;
    reviewCount: number;
    newReviews: number;
    occupancyPercent: number | null;
    services: StatsRow[];
    employees: StatsRow[];
    weekdays: number[]; // Monday first
    hours: number[]; // 0-23 in Oslo
    weeks: { weekStart: string; visits: number; revenue: number }[];
    tips: StatsTip[];
    weeklyReportOn: boolean;
    proInterest: boolean;
};

// "Favoritter" in Min Besti
export type Favorite = {
    businessId: string;
    name: string;
    slug: string;
    city: string;
    category: string;
    logoUrl: string | null;
    averageRating: number | null;
    reviewCount: number;
    lastVisit: {
        startUtc: string;
        serviceId: string;
        serviceName: string;
        employeeId: string;
        employeeName: string;
        serviceAvailable: boolean;
        employeeAvailable: boolean;
    } | null;
    nextVisitUtc: string | null;
};

// /admin (BESTI team only)
export type AdminBusinessRow = {
    id: string;
    name: string;
    slug: string;
    city: string;
    category: string;
    status: "Venter" | "Synlig" | "Ubekreftet" | "Stengt" | "Skjult";
    createdUtc: string;
    ownerEmail: string | null;
    phoneNumber: string;
    verified: boolean;
    setupDone: number;
    setupTotal: number;
    bookingsLast30Days: number;
    upcoming: number;
    averageStars: number | null;
    proInterest: boolean;
    hasNote: boolean;
};

export type AdminOverview = {
    visibleBusinesses: number;
    waitingForApproval: number;
    unconfirmedSignups: number;
    customerAccounts: number;
    bookingsLast7Days: number;
    visitsLast30Days: number;
    reviewsLast30Days: number;
    proInterested: number;
    waiting: AdminBusinessRow[];
    needHelp: AdminBusinessRow[];
    weeks: AdminWeek[];
    proLeads: AdminBusinessRow[];
};

// "Utvikling": one row per week (Monday), the newest last
export type AdminWeek = { weekStart: string; newBusinesses: number; newCustomers: number; bookings: number };

export type AdminAction = { createdUtc: string; adminEmail: string; text: string; businessId: string | null };

export type AdminBusinessDetails = {
    business: AdminBusinessRow;
    orgNumber: string;
    address: string;
    postalCode: string;
    publicEmail: string | null;
    website: string | null;
    referralSource: string | null;
    approvedUtc: string | null;
    adminNote: string | null;
    weeklyReportOn: boolean;
    reviewCount: number;
    lastBookingCreatedUtc: string | null;
    setup: { title: string; done: boolean }[];
    suspendedUtc: string | null;
    suspendedReason: string | null;
    log: AdminAction[] | null;
};

export type AdminReasons = { review: string[]; suspend: string[] };

export type AdminMail = { to: string; subject: string; createdUtc: string; attempts: number; nextAttemptUtc: string };

// /admin/kunder: one customer, found by the exact e-mail or phone number
export type AdminCustomer = {
    id: string;
    email: string;
    fullName: string | null;
    phone: string | null;
    emailConfirmed: boolean;
    hasPassword: boolean;
    hasGoogle: boolean;
    createdUtc: string | null;
    visits: number;
    reviews: number;
    upcoming: { id: string; businessName: string; serviceName: string; startUtc: string; status: string }[];
    waitingMails: AdminMail[];
    staffAt: string[];
};

export type AdminHealth = {
    version: string | null;
    startedUtc: string;
    databaseOk: boolean;
    databaseSizeMb: number | null;
    lastBackupUtc: string | null;
    mailsWaiting: number;
    mailsFailing: number;
    failingMails: AdminMail[];
    pushDevices: number;
    jobs: { name: string; lastRunUtc: string; ok: boolean }[];
};

export type AdminReview = {
    id: string;
    businessName: string;
    businessSlug: string;
    businessId: string;
    customerName: string;
    stars: number;
    comment: string | null;
    businessReply: string | null;
    createdUtc: string;
    photoUrl: string | null;
    hiddenUtc: string | null;
    hiddenReason: string | null;
};

// "Kunder" in the panel. key: "k-…" (customer account) or "t-…" (phone number)
export type CustomerRow = {
    key: string;
    name: string;
    phone: string;
    email: string | null;
    hasAccount: boolean;
    visits: number;
    noShows: number;
    lastVisitUtc: string | null;
    nextVisitUtc: string | null;
    hasNote: boolean;
    // The business's own note, shown in the notepad on Kunder (needs the API to send it; null = no note)
    note?: string | null;
    // Brought from the business's old system ("Importer kunder") and not booked here yet
    imported: boolean;
};

// "Importer kunder": what the API found in the list (saved = false is only the check before "Importer")
export type CustomerImportResult = {
    saved: boolean;
    added: number;
    existing: number;
    duplicates: number;
    skipped: number;
    notesAdded: number;
    problems: { line: number; message: string }[];
};

export type CustomerCard = {
    customer: CustomerRow;
    note: string | null;
    noteUpdatedUtc: string | null;
    spentNok: number;
    visits: { id: string; startUtc: string; serviceName: string; employeeName: string; status: string;
        priceNok: number | null; customerNote: string | null }[];
};

// Panel → Ansatte: who has their own login (connected = accepted)
export type StaffAccess = { employeeId: string; email: string | null; connected: boolean; inviteExpiresUtc: string | null };

export type StaffInviteInfo = { businessName: string; employeeName: string; email: string };

// /ansatt: one place of work with the employee's own visits
export type StaffPlace = {
    employeeId: string;
    businessName: string;
    businessSlug: string;
    employeeName: string;
    bookings: { id: string; startUtc: string; endUtc: string; status: string; serviceName: string;
        customerName: string; customerPhone: string; customerNote: string | null; salonNote: string | null }[];
    blocks: { startUtc: string; endUtc: string; reason: string | null }[];
};

// "Stempelkort": stamps on the current round (0..visits); rewardReady = the business can give the reward now
export type LoyaltyCard = {
    businessId: string;
    businessName: string;
    businessSlug: string;
    visits: number;
    reward: string;
    stamps: number;
    rewardReady: boolean;
    rewardsGiven: number;
};

export type PanelLoyalty = {
    visits: number | null;
    reward: string | null;
    startUtc: string | null;
    customers: {
        customerUserId: string;
        name: string;
        stamps: number;
        rewardReady: boolean;
        lastVisitUtc: string;
        rewardsGiven: number;
    }[];
};

// "Mitt BESTI-år"
export type MyYear = {
    year: number;
    visits: number;
    businesses: number;
    minutesOfMeTime: number;
    spent: number;
    visitsWithFriends: number;
    reviewsWritten: number;
    favoriteBusiness: string | null;
    favoriteBusinessSlug: string | null;
    favoriteBusinessVisits: number;
    favoriteService: string | null;
    busiestMonth: number | null;
    firstVisitUtc: string | null;
    months: number[];
    years: number[];
};

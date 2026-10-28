// BusinessListItemDTO type definition!!!
export type BusinessListItem = {
    id: string;
    name: string;
    category: string;
    description: string | null;
    city: string;
    logoUrl: string | null;
    freeSlots: FreeSlot[] | null; // only when searching with a date
    distanceKm: number | null; // only with "Nær meg"
    averageRating: number | null; // e.g. 4.7; null when nobody has reviewed yet
    reviewCount: number;
};

export type FreeSlot = {
    startUtc: string;
    serviceId: string;
    serviceName: string;
    employeeId: string;
};
// One day in the "Velg dag" strip; employeeId = who is free at that time
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
};

export type BusinessDetails = {
    id: string;
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
    businessAddress: string;
    serviceId: string;
    employeeId: string;
    isReviewed: boolean; // the customer has already reviewed this visit
    businessPhone: string;
    freeCancelHours: number;
    previousStartUtc: string | null; // set while the business waits for the customer to accept a new time
};

export type Review = {
    id: string;
    stars: number;
    comment: string | null;
    customerName: string;
    createdUtc: string;
    businessReply: string | null;
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

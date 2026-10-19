// BusinessListItemDTO type definition!!!
export type BusinessListItem = {
    id: string;
    name: string;
    category: string;
    description: string | null;
    city: string;
    logoUrl: string | null;
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
    email: string;
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
};
// BusinessListItemDTO type definition!!!
export type BusinessListItem = {
    id: string;
    name: string;
    category: string;
    description: string | null;
    city: string;
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
};
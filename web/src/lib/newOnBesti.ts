// "Ny på Besti": the stamp on the business page the first week after the business went live
const NEW_DAYS = 7;

export function isNewOnBesti(joinedUtc: string | null, now = Date.now()): boolean {
    if (!joinedUtc) return false;
    const age = now - new Date(joinedUtc).getTime();
    return age >= 0 && age < NEW_DAYS * 24 * 60 * 60 * 1000;
}

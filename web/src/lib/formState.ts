// What a form action gives back when something went wrong:
// the message to show and what the user typed (never the password)
export type FormState = {

    error?: string;

    values?: Record<string, string>;
};
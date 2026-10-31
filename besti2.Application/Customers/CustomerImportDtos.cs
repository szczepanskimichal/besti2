namespace besti2.Application.Customers;

// "Importer kunder": the rows of a CSV file from another system, after the business chose which column is what.
// Line = the line in the file, so a problem can say where it is
public record CustomerImportRow(int Line, string? Name, string? Phone, string? Email, string? Note);

// Save = false only checks the rows and counts (the preview before "Importer")
public record CustomerImportRequest(IReadOnlyList<CustomerImportRow>? Rows, bool Save, bool Confirmed);

public record CustomerImportProblem(int Line, string Message);

// Added = new in Kunder; Existing = already a customer here (from a booking or an earlier import);
// Duplicates = the same phone again in the file; Skipped = rows without a name or a valid phone;
// Problems = the first ones with their line, also rows imported without a bad e-mail
public record CustomerImportResultDto(
    bool Saved,
    int Added,
    int Existing,
    int Duplicates,
    int Skipped,
    int NotesAdded,
    IReadOnlyList<CustomerImportProblem> Problems);

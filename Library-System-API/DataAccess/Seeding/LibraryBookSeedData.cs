namespace LibrarySystem.DataAccess.Seeding;

/// <summary>
/// A single seed book definition.
/// </summary>
/// <param name="Title">Book title.</param>
/// <param name="Author">Primary author.</param>
/// <param name="Isbn">Real ISBN-13 where confidently known; otherwise null.</param>
/// <param name="Category">Library category/genre.</param>
/// <param name="TotalCopies">Total physical copies owned by the library.</param>
/// <param name="AvailableCopies">Copies currently on the shelf (may be 0).</param>
public sealed record LibraryBookSeed(
    string Title,
    string Author,
    string? Isbn,
    string Category,
    int TotalCopies,
    int AvailableCopies);

/// <summary>
/// Static source of realistic catalog seed data: real-world books a
/// university/public library would hold, with tiered inventory
/// (popular 5–10, normal 2–5, specialized 1–3) and a mixture of availability
/// states including fully borrowed titles. Constraints guaranteed by the data:
/// unique title/author, 1 ≤ TotalCopies, 0 ≤ AvailableCopies ≤ TotalCopies.
/// </summary>
public static class LibraryBookSeedData
{
    /// <summary>
    /// Gets the seed book definitions.
    /// </summary>
    public static IReadOnlyList<LibraryBookSeed> Books { get; } =
    [
        // ── Classics ────────────────────────────────────────────────────────
        new("To Kill a Mockingbird", "Harper Lee", "9780061120084", "Classics", 6, 4),
        new("1984", "George Orwell", "9780451524935", "Classics", 8, 3),
        new("The Great Gatsby", "F. Scott Fitzgerald", "9780743273565", "Classics", 5, 2),
        new("Pride and Prejudice", "Jane Austen", "9780141439518", "Classics", 4, 4),
        new("The Catcher in the Rye", "J.D. Salinger", "9780316769488", "Classics", 4, 0),

        // ── Science Fiction ────────────────────────────────────────────────
        new("Fahrenheit 451", "Ray Bradbury", "9781451673319", "Science Fiction", 5, 3),
        new("Brave New World", "Aldous Huxley", "9780060850524", "Science Fiction", 4, 2),
        new("Dune", "Frank Herbert", "9780441013593", "Science Fiction", 7, 2),
        new("Ender's Game", "Orson Scott Card", "9780812550702", "Science Fiction", 3, 1),
        new("The Martian", "Andy Weir", "9780553418026", "Science Fiction", 6, 5),

        // ── Fantasy ────────────────────────────────────────────────────────
        new("The Hobbit", "J.R.R. Tolkien", "9780547928227", "Fantasy", 8, 5),
        new("The Fellowship of the Ring", "J.R.R. Tolkien", "9780547928210", "Fantasy", 6, 2),
        new("Harry Potter and the Sorcerer's Stone", "J.K. Rowling", "9780590353427", "Fantasy", 10, 4),
        new("A Game of Thrones", "George R.R. Martin", "9780553103540", "Fantasy", 7, 0),
        new("The Name of the Wind", "Patrick Rothfuss", "9780756404741", "Fantasy", 4, 3),
        new("Mistborn: The Final Empire", "Brandon Sanderson", "9780765311786", "Fantasy", 3, 2),

        // ── Mystery & Thriller ─────────────────────────────────────────────
        new("Murder on the Orient Express", "Agatha Christie", "9780062693662", "Mystery", 5, 3),
        new("And Then There Were None", "Agatha Christie", "9780062073488", "Mystery", 4, 1),
        new("The Adventures of Sherlock Holmes", "Arthur Conan Doyle", "9780553212419", "Mystery", 3, 2),
        new("In the Woods", "Tana French", null, "Mystery", 2, 2),
        new("Gone Girl", "Gillian Flynn", "9780307588364", "Thriller", 6, 1),
        new("The Girl with the Dragon Tattoo", "Stieg Larsson", "9780307454546", "Thriller", 5, 0),
        new("The Da Vinci Code", "Dan Brown", "9780307474278", "Thriller", 7, 4),
        new("The Silent Patient", "Alex Michaelides", "9781250301697", "Thriller", 4, 2),
        new("The Girl on the Train", "Paula Hawkins", "9781594633669", "Thriller", 3, 1),

        // ── Fiction & Historical Fiction ───────────────────────────────────
        new("The Kite Runner", "Khaled Hosseini", "9781594631931", "Historical Fiction", 6, 3),
        new("A Thousand Splendid Suns", "Khaled Hosseini", "9781594483851", "Fiction", 5, 2),
        new("All the Light We Cannot See", "Anthony Doerr", "9781476746586", "Historical Fiction", 4, 2),
        new("The Nightingale", "Kristin Hannah", "9780312577460", "Historical Fiction", 4, 0),
        new("The Book Thief", "Markus Zusak", "9780375842207", "Young Adult", 5, 3),
        new("The Alchemist", "Paulo Coelho", "9780062315007", "Fiction", 6, 6),
        new("Life of Pi", "Yann Martel", "9780156027328", "Fiction", 3, 1),
        new("The Road", "Cormac McCarthy", "9780307387899", "Fiction", 2, 1),

        // ── Biography & History ────────────────────────────────────────────
        new("Educated", "Tara Westover", "9780399590504", "Biography", 5, 2),
        new("Steve Jobs", "Walter Isaacson", "9781451648539", "Biography", 3, 1),
        new("The Diary of a Young Girl", "Anne Frank", "9780553296983", "Biography", 4, 3),
        new("Long Walk to Freedom", "Nelson Mandela", "9780316548182", "Biography", 2, 1),
        new("Sapiens: A Brief History of Humankind", "Yuval Noah Harari", "9780062316097", "History", 8, 3),
        new("Guns, Germs, and Steel", "Jared Diamond", "9780393354324", "History", 3, 2),
        new("The Guns of August", "Barbara W. Tuchman", "9780345476098", "History", 1, 1),

        // ── Science ────────────────────────────────────────────────────────
        new("A Brief History of Time", "Stephen Hawking", "9780553380163", "Science", 5, 2),
        new("Cosmos", "Carl Sagan", "9780345539434", "Science", 3, 2),
        new("The Selfish Gene", "Richard Dawkins", "9780198788607", "Science", 2, 0),

        // ── Computer Science & Technology ──────────────────────────────────
        new("Clean Code", "Robert C. Martin", "9780132350884", "Computer Science", 6, 2),
        new("Clean Architecture", "Robert C. Martin", "9780134494166", "Computer Science", 4, 3),
        new("The Pragmatic Programmer", "Andrew Hunt", "9780201616224", "Computer Science", 4, 1),
        new("Design Patterns: Elements of Reusable Object-Oriented Software",
            "Erich Gamma", "9780201633610", "Computer Science", 3, 1),
        new("Introduction to Algorithms", "Thomas H. Cormen", "9780262033848", "Computer Science", 3, 2),
        new("Cracking the Coding Interview", "Gayle Laakmann McDowell", "9780984782857",
            "Computer Science", 5, 0),
        new("Artificial Intelligence: A Modern Approach", "Stuart Russell", "9780134610993",
            "Computer Science", 2, 1),
        new("The Phoenix Project", "Gene Kim", "9780988262591", "Technology", 3, 2),
        new("The Mythical Man-Month", "Frederick P. Brooks Jr.", "9780201835953", "Technology", 2, 1),

        // ── Business ───────────────────────────────────────────────────────
        new("Zero to One", "Peter Thiel", "9780804139298", "Business", 4, 2),
        new("Rich Dad Poor Dad", "Robert T. Kiyosaki", "9781612680194", "Business", 5, 4),
        new("Good to Great", "Jim Collins", "9780066620992", "Business", 3, 1),

        // ── Psychology & Philosophy ────────────────────────────────────────
        new("Thinking, Fast and Slow", "Daniel Kahneman", "9780374533557", "Psychology", 4, 2),
        new("Man's Search for Meaning", "Viktor E. Frankl", "9780807014295", "Psychology", 5, 3),
        new("Influence: The Psychology of Persuasion", "Robert B. Cialdini", "9780061245455",
            "Psychology", 3, 1),
        new("Meditations", "Marcus Aurelius", "9780140449334", "Philosophy", 3, 2),
        new("Sophie's World", "Jostein Gaarder", "9780374530716", "Philosophy", 2, 1),
        new("Thus Spoke Zarathustra", "Friedrich Nietzsche", null, "Philosophy", 1, 1),

        // ── Self-Help ──────────────────────────────────────────────────────
        new("Atomic Habits", "James Clear", "9780735211292", "Self-Help", 9, 3),
        new("The 7 Habits of Highly Effective People", "Stephen R. Covey", "9781982137274",
            "Self-Help", 5, 2)
    ];
}

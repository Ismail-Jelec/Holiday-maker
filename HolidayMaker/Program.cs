using MySqlConnector;

const string ConnString = "Server=localhost;Database=holiday_maker;User=root;Password=kurslosenord;";
int? currentCustomerId = null;

while (true)
{
    Console.WriteLine();
    Console.WriteLine("| HOLIDAY MAKER |");
    Console.WriteLine(currentCustomerId is null ? "No customer selected" : $"Selected customer: #{currentCustomerId}");
    Console.WriteLine("1. Register new customer");
    Console.WriteLine("2. Choose customer");
    Console.WriteLine("3. Search and book room");
    Console.WriteLine("4. Show customer's bookings");
    Console.WriteLine("5. Change number of people");
    Console.WriteLine("6. Cancel booking");
    Console.WriteLine("0. Exit");

    int choice = ReadInt("Choice: ", 0, 6);

    try
    {
        switch (choice)
        {
            case 1: RegisterCustomer(); break;
            case 2: ChooseCustomer(); break;
            case 3: SearchAndBook(); break;
            case 4: CustomerBooking(); break;
            case 5: ChangeNumber(); break;
            case 6: Cancel(); break;
            case 0: return;
        }
    }
    catch (MySqlException ex)
    {
        // A database problem shouldn't crash the program
        Console.WriteLine($"Database error: {ex.Message}");
    }
}

void RegisterCustomer()
{
    string first_name = ReadText("First name: ");
    string last_name = ReadText("Last name: ");
    string email = ReadText("Email: ");
    string phone = ReadText("Phone number: ");

    using var conn = new MySqlConnection(ConnString);
    conn.Open();

    using var cmd = new MySqlCommand("INSERT INTO customer (first_name, last_name, email, phone) " + "VALUES (@first_name, @last_name, @email, @phone)", conn);
    cmd.Parameters.AddWithValue("@first_name", first_name);
    cmd.Parameters.AddWithValue("@last_name", last_name);
    cmd.Parameters.AddWithValue("@email", email);
    cmd.Parameters.AddWithValue("@phone", phone);

    try
    {
        cmd.ExecuteNonQuery();
    }
    catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
    {
        Console.WriteLine("\nA customer with that email already exists");
        return;
    }

    currentCustomerId = (int)cmd.LastInsertedId;
    Console.WriteLine($"Customer registered with id {currentCustomerId} and selected.");
}

void ChooseCustomer()
{
    using var conn = new MySqlConnection(ConnString);
    conn.Open();

    using (var list = new MySqlCommand(
        "SELECT customer_id, first_name, last_name, email FROM customer ORDER BY customer_id", conn))
    using (var reader = list.ExecuteReader())
    {
        while (reader.Read())
            Console.WriteLine($"{reader.GetInt32(0),3}: {reader.GetString(1)} {reader.GetString(2)} ({reader.GetString(3)})");
    }

    int id = ReadInt("Customer id: ", 1, int.MaxValue);

    using var check = new MySqlCommand("SELECT COUNT(*) FROM customer WHERE customer_id = @id", conn);
    check.Parameters.AddWithValue("@id", id);

    if (Convert.ToInt32(check.ExecuteScalar()) == 0)
    {
        Console.WriteLine("No customer with that id.");
        return;
    }

    currentCustomerId = id;
    Console.WriteLine($"Customer #{id} selected.");
}

int ReadInt(string prompt, int min, int max)
{
    while (true)
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max)
            return value;
        Console.WriteLine($"Please enter a number between {min} and {max}.");
    }
}

string ReadText(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        string? input = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(input))
            return input;
        Console.WriteLine("This field can't be empty.");
    }
}

DateTime ReadDate(string prompt, DateTime min, DateTime max)
{
    while (true)
    {
        Console.Write(prompt);
        if (!DateTime.TryParseExact(Console.ReadLine(), "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out DateTime date))
        {
            Console.WriteLine("Please use the format YYYY-MM-DD, e.g. 2026-12-10.");
            continue;
        }
        if (date < min || date > max)
        {
            Console.WriteLine($"Date must be between {min:yyyy-MM-dd} and {max:yyyy-MM-dd}.");
            continue;
        }
        return date;
    }
}

bool ReadYesNo(string prompt)
{
    while (true)
    {
        Console.Write(prompt + " (y/n): ");
        string? input = Console.ReadLine()?.Trim().ToLower();
        if (input == "y") return true;
        if (input == "n") return false;
        Console.WriteLine("Please answer y or n.");
    }
}

void SearchAndBook()
{
    if (currentCustomerId == null)
    {
        Console.WriteLine("Choose a customer first");
        return;
    }

    string city = ReadText("City: ");
    // Rooms can only be booked during December 2026; check-out may be the morning of 1 January.
    // Check-out is asked again until it is after check-in, so the user doesn't lose the other answers.
    DateTime checkIn = ReadDate("Check-in (YYYY-MM-DD): ", new DateTime(2026, 12, 1), new DateTime(2026, 12, 31));
    DateTime checkOut = ReadDate("Check-out (YYYY-MM-DD): ", checkIn.AddDays(1), new DateTime(2027, 1, 1));
    int people = ReadInt("Number of people: ", 1, 20);

    bool needPool = ReadYesNo("Must have pool?");
    bool needRestaurant = ReadYesNo("Must have restaurant?");
    int maxBeach = ReadInt("Max distance to beach in metres (0 = no limit): ", 0, 100000);
    int maxCentre = ReadInt("Max distance to centre in metres (0 = no limit): ", 0, 100000);
    int sort = ReadInt("Sort by 1 = price (lowest first), 2 = rating (highest first): ", 1, 2);

    using var conn = new MySqlConnection(ConnString);
    conn.Open();

    string sql = @"
        SELECT r.room_id, a.name, a.city, r.room_number, r.max_people, r.price_per_night
        FROM room r
        JOIN accommodation a ON r.accommodation_id = a.accommodation_id
        WHERE a.city = @city
          AND r.max_people >= @people
          AND NOT EXISTS (
              SELECT 1
              FROM booking b
              WHERE b.room_id = r.room_id
                AND b.check_in  < @checkOut
                AND b.check_out > @checkIn
          )";
    if (needPool)       sql += " AND a.pool = TRUE";
    if (needRestaurant) sql += " AND a.restaurant = TRUE";
    if (maxBeach > 0)   sql += " AND a.distance_beach <= @maxBeach";
    if (maxCentre > 0)  sql += " AND a.distance_city <= @maxCentre";

    sql += sort == 1
        ? " ORDER BY r.price_per_night ASC"
        : " ORDER BY a.rating DESC, r.price_per_night ASC";

    using var cmd = new MySqlCommand(sql, conn);

    cmd.Parameters.AddWithValue("@city", city);
    cmd.Parameters.AddWithValue("@people", people);
    cmd.Parameters.AddWithValue("@checkIn", checkIn);
    cmd.Parameters.AddWithValue("@checkOut", checkOut);

    if (maxBeach > 0)  cmd.Parameters.AddWithValue("@maxBeach", maxBeach);
    if (maxCentre > 0) cmd.Parameters.AddWithValue("@maxCentre", maxCentre);

    using var reader = cmd.ExecuteReader();

    Console.WriteLine("\nRoom id | Hotel | City | Room | Max people | Price/night");
    int count = 0;
    while (reader.Read())
    {
        count++;
        Console.WriteLine($"{reader.GetInt32(0)} | {reader.GetString(1)} | {reader.GetString(2)} | " +
                          $"{reader.GetString(3)} | {reader.GetInt32(4)} | {reader.GetDecimal(5)} kr");
    }

    if (count == 0)
        Console.WriteLine("No available rooms found.");

    if (count == 0)
        return;

    reader.Close();

    int roomId = ReadInt("\nRoom id to book (0 = don't book): ", 0, int.MaxValue);
    if (roomId == 0)
        return;

    using var check = new MySqlCommand(@"
        SELECT r.price_per_night
        FROM room r
        WHERE r.room_id = @roomId
          AND r.max_people >= @people
          AND NOT EXISTS (
              SELECT 1
              FROM booking b
              WHERE b.room_id = r.room_id
                AND b.check_in  < @checkOut
                AND b.check_out > @checkIn
          )", conn);
    check.Parameters.AddWithValue("@roomId", roomId);
    check.Parameters.AddWithValue("@people", people);
    check.Parameters.AddWithValue("@checkIn", checkIn);
    check.Parameters.AddWithValue("@checkOut", checkOut);

    object? result = check.ExecuteScalar();
    if (result == null)
    {
        Console.WriteLine("That room is not available for these dates or doesn't fit that many people.");
        return;
    }
    decimal pricePerNight = (decimal)result;

    using var insert = new MySqlCommand(@"
        INSERT INTO booking (customer_id, room_id, check_in, check_out, number_of_people)
        VALUES (@customerId, @roomId, @checkIn, @checkOut, @people)", conn);
    insert.Parameters.AddWithValue("@customerId", currentCustomerId);
    insert.Parameters.AddWithValue("@roomId", roomId);
    insert.Parameters.AddWithValue("@checkIn", checkIn);
    insert.Parameters.AddWithValue("@checkOut", checkOut);
    insert.Parameters.AddWithValue("@people", people);
    insert.ExecuteNonQuery();

    int nights = (checkOut - checkIn).Days;
    Console.WriteLine($"\nBooking confirmed! Booking number: {insert.LastInsertedId}");
    Console.WriteLine($"{nights} nights x {pricePerNight} kr = {nights * pricePerNight} kr");
}

void CustomerBooking()
{
    if (currentCustomerId == null)
    {
        Console.WriteLine("No customer is selected");
        return;
    }

    using var conn = new MySqlConnection(ConnString);
    conn.Open();

    using var cmd = new MySqlCommand(@"
        SELECT b.booking_id, a.name, r.room_number, b.check_in, b.check_out, b.number_of_people,
               DATEDIFF(b.check_out, b.check_in) * r.price_per_night AS total_price
        FROM booking b
        JOIN room r          ON b.room_id = r.room_id
        JOIN accommodation a ON r.accommodation_id = a.accommodation_id
        WHERE b.customer_id = @customerId
        ORDER BY b.check_in", conn);

    cmd.Parameters.AddWithValue("@customerId", currentCustomerId);

    using var reader = cmd.ExecuteReader();

    Console.WriteLine("\nHere are your booked rooms:");
    Console.WriteLine("Booking | Hotel | Room | Check-in | Check-out | People | Total");
    int count = 0;
    while (reader.Read())
    {
        count++;
        Console.WriteLine($"{reader.GetInt32(0)} | {reader.GetString(1)} | {reader.GetString(2)} | " +
                          $"{reader.GetDateTime(3):yyyy-MM-dd} | {reader.GetDateTime(4):yyyy-MM-dd} | " +
                          $"{reader.GetInt32(5)} | {reader.GetDecimal(6)} kr");
    }

    if (count == 0)
        Console.WriteLine("This customer has no bookings.");
}

void ChangeNumber()
{
    if (currentCustomerId == null)
    {
        Console.WriteLine("No customer is selected");
        return;
    }

    CustomerBooking();

    int bookingId = ReadInt("Booking number to change (0 = back): ", 0, int.MaxValue);
    if (bookingId == 0)
        return;

    int people = ReadInt("New number of people: ", 1, 20);

    using var conn = new MySqlConnection(ConnString);
    conn.Open();

    using var cmd = new MySqlCommand(@"
        UPDATE booking b
        JOIN room r ON b.room_id = r.room_id
        SET b.number_of_people = @people
        WHERE b.booking_id = @bookingId
          AND b.customer_id = @customerId
          AND r.max_people >= @people", conn);

    cmd.Parameters.AddWithValue("@bookingId", bookingId);
    cmd.Parameters.AddWithValue("@customerId", currentCustomerId);
    cmd.Parameters.AddWithValue("@people", people);

    int rows = cmd.ExecuteNonQuery();

    if (rows == 0)
        Console.WriteLine("No such booking for this customer, or the room can't fit that many people.");
    else
        Console.WriteLine("Number of people updated.");
}

void Cancel()
{
    if (currentCustomerId == null)
    {
        Console.WriteLine("No customer is selected");
        return;
    }

    CustomerBooking();

    int bookingId = ReadInt("Booking number to cancel (0 = back): ", 0, int.MaxValue);
    if (bookingId == 0)
        return;

    using var conn = new MySqlConnection(ConnString);
    conn.Open();

    using var cmd = new MySqlCommand(@"
        DELETE FROM booking
        WHERE booking_id = @bookingId
          AND customer_id = @customerId", conn);

    cmd.Parameters.AddWithValue("@bookingId", bookingId);
    cmd.Parameters.AddWithValue("@customerId", currentCustomerId);

    int rows = cmd.ExecuteNonQuery();

    if (rows == 0)
        Console.WriteLine("No booking with that number for this customer.");
    else
        Console.WriteLine("Booking cancelled.");
}
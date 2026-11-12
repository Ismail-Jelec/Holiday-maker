USE holiday_maker;

SET @city = 'Dubrovnik', @people = 2,
    @checkIn = '2026-12-10', @checkOut = '2026-12-15',
    @customerId = 1, @roomId = 1, @bookingId = 1;

INSERT INTO customer (first_name, last_name, email, phone)
VALUES ('Test', 'Testsson', 'test.testsson@example.com', '070-0000000');

SELECT customer_id, first_name, last_name, email
FROM customer
ORDER BY customer_id;

SELECT COUNT(*) FROM customer WHERE customer_id = @customerId;

SELECT r.room_id, a.name, a.city, r.room_number, r.max_people, r.price_per_night
FROM room r
         JOIN accommodation a ON r.accommodation_id = a.accommodation_id
WHERE a.city = @city
  AND r.max_people >= @people
  AND NOT EXISTS (SELECT 1
                  FROM booking b
                  WHERE b.room_id = r.room_id
                    AND b.check_in < @checkOut
                    AND b.check_out > @checkIn)
ORDER BY r.price_per_night ASC;

SELECT r.room_id, a.name, a.city, r.room_number, r.max_people, r.price_per_night, a.rating
FROM room r
         JOIN accommodation a ON r.accommodation_id = a.accommodation_id
WHERE a.city = @city
  AND r.max_people >= @people
  AND NOT EXISTS (SELECT 1
                  FROM booking b
                  WHERE b.room_id = r.room_id
                    AND b.check_in < @checkOut
                    AND b.check_out > @checkIn)
  AND a.pool = TRUE
  AND a.restaurant = TRUE
  AND a.distance_beach <= 200
  AND a.distance_city <= 1500
ORDER BY a.rating DESC, r.price_per_night ASC;


SELECT a.name, a.city, r.room_number, r.max_people, r.price_per_night
FROM room r
         JOIN accommodation a ON r.accommodation_id = a.accommodation_id
WHERE a.city = 'Makarska'
  AND r.max_people >= 4
  AND NOT EXISTS (SELECT 1
                  FROM booking b
                  WHERE b.room_id = r.room_id
                    AND b.check_in < '2026-12-27'
                    AND b.check_out > '2026-12-24');

SELECT r.price_per_night
FROM room r
WHERE r.room_id = @roomId
  AND r.max_people >= @people
  AND NOT EXISTS (SELECT 1
                  FROM booking b
                  WHERE b.room_id = r.room_id
                    AND b.check_in < @checkOut
                    AND b.check_out > @checkIn);

INSERT INTO booking (customer_id, room_id, check_in, check_out, number_of_people)
VALUES (@customerId, @roomId, @checkIn, @checkOut, @people);

SELECT b.booking_id,
       a.name,
       r.room_number,
       b.check_in,
       b.check_out,
       b.number_of_people,
       DATEDIFF(b.check_out, b.check_in) * r.price_per_night AS total_price
FROM booking b
         JOIN room r ON b.room_id = r.room_id
         JOIN accommodation a ON r.accommodation_id = a.accommodation_id
WHERE b.customer_id = @customerId
ORDER BY b.check_in;

UPDATE booking b
    JOIN room r ON b.room_id = r.room_id
SET b.number_of_people = @people
WHERE b.booking_id = @bookingId
  AND b.customer_id = @customerId
  AND r.max_people >= @people;

DELETE
FROM booking
WHERE booking_id = @bookingId
  AND customer_id = @customerId;

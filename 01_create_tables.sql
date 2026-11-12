USE holiday_maker;


CREATE TABLE customer
(
    customer_id INT AUTO_INCREMENT PRIMARY KEY,
    first_name VARCHAR(50) NOT NULL,
    last_name  VARCHAR(50) NOT NULL,
    email      VARCHAR(100) NOT NULL UNIQUE,
    phone      VARCHAR(20) NOT NULL
);

CREATE TABLE accommodation
(
    accommodation_id INT AUTO_INCREMENT PRIMARY KEY,
    name       VARCHAR(50) NOT NULL,
    city       VARCHAR(100) NOT NULL,
    pool       BOOLEAN NOT NULL DEFAULT FALSE,
    restaurant BOOLEAN NOT NULL DEFAULT FALSE,
    distance_beach   INT NOT NULL CHECK (distance_beach >= 0),
    distance_city   INT NOT NULL CHECK (distance_city >= 0),
    rating     TINYINT NOT NULL CHECK (rating BETWEEN 1 AND 5)
);

CREATE TABLE room
(
    room_id          INT AUTO_INCREMENT PRIMARY KEY,
    accommodation_id INT NOT NULL,
    room_number      VARCHAR(10) NOT NULL,
    max_people       INT NOT NULL CHECK (max_people >= 1),
    price_per_night  DECIMAL(8,2) NOT NULL CHECK (price_per_night >= 1),
    FOREIGN KEY (accommodation_id) REFERENCES accommodation(accommodation_id),
    UNIQUE (accommodation_id, room_number)
);

CREATE TABLE booking
(
    booking_id       INT AUTO_INCREMENT PRIMARY KEY,
    customer_id      INT NOT NULL,
    room_id          INT NOT NULL,
    check_in         DATE NOT NULL,
    check_out        DATE NOT NULL,
    number_of_people INT NOT NULL CHECK (number_of_people >= 1),
    FOREIGN KEY      (customer_id) REFERENCES customer(customer_id),
    FOREIGN KEY      (room_id)     REFERENCES room(room_id),
    CHECK (check_out > check_in)
);
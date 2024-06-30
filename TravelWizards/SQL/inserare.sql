-- Utilizarea funcției create_company pentru inserarea datelor în 'companies'

CALL create_company('TravelX', '#FFFFFF', '#000000');
CALL create_company('TransportY', '#000000', '#FFFFFF');
CALL create_company('BoardingZ', '#FF0000', '#00FF00');

-- Introducere date pentru 'locations'
INSERT INTO locations (full_name, abbreviation) VALUES
('Iasi', 'IAS'),
('Bucuresti', 'BUC'),
('Cluj-Napoca', 'CLJ'),
('Timisoara', 'TIM'),
('Sibiu', 'SIB'),
('Constanta', 'CST'),
('Brasov', 'BSV');

-- Utilizarea funcției create_user pentru inserarea datelor în 'users'

CALL create_user('agent@travelx.com', 'pass123', 'TravelX', 'travel_agent_role', 'Agent TravelX');
CALL create_user('manager@transporty.com', 'pass123', 'TransportY', 'transportation_company_role', 'Manager TransportY');
CALL create_user('checker@boardingz.com', 'pass123', 'BoardingZ', 'boarding_agent_role', 'Checker BoardingZ');

-- Introducere date pentru 'travel_routes'
INSERT INTO travel_routes (departure_location_id, arrival_location_id, transport_type) VALUES
((SELECT location_id FROM locations WHERE abbreviation = 'IAS'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'bus'),
((SELECT location_id FROM locations WHERE abbreviation = 'CLJ'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'train'),
((SELECT location_id FROM locations WHERE abbreviation = 'SIB'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'train'),
((SELECT location_id FROM locations WHERE abbreviation = 'TIM'), (SELECT location_id FROM locations WHERE abbreviation = 'CST'), 'ship'),
((SELECT location_id FROM locations WHERE abbreviation = 'CST'), (SELECT location_id FROM locations WHERE abbreviation = 'BSV'), 'airplane'),
((SELECT location_id FROM locations WHERE abbreviation = 'BSV'), (SELECT location_id FROM locations WHERE abbreviation = 'BUC'), 'bus');

-- Introducere date pentru 'route_schedules'
INSERT INTO route_schedules (route_id, departure_time, arrival_time, price, frequency, valid_from, valid_until) VALUES
(1, '2024-06-01 06:00:00', '2024-06-01 10:00:00', 45, '7 days'::interval, '2024-06-01', '2024-12-31'),
(2, '2024-06-01 09:00:00', '2024-06-01 13:00:00', 75, '7 days'::interval, '2024-06-01', '2024-12-31'),
(3, '2024-06-01 15:00:00', '2024-06-01 19:00:00', 50, '7 days'::interval, '2024-06-01', '2024-12-31'),
(4, '2024-06-02 08:00:00', '2024-06-02 14:00:00', 80, '14 days'::interval, '2024-06-02', '2024-12-31'),
(5, '2024-06-03 10:00:00', '2024-06-03 11:30:00', 100, '1 month'::interval, '2024-06-03', '2024-12-31'),
(6, '2024-06-04 12:00:00', '2024-06-04 15:00:00', 40, '1 month'::interval, '2024-06-04', '2024-12-31');

-- Introducere date pentru 'bookings'
INSERT INTO bookings (user_id, first_name, last_name, phone_number) VALUES
((SELECT user_id FROM users WHERE email = 'agent@travelx.com'), 'John', 'Doe', '+40700123456'),
((SELECT user_id FROM users WHERE email = 'manager@transporty.com'), 'Jane', 'Smith', '+40700987654'),
((SELECT user_id FROM users WHERE email = 'checker@boardingz.com'), 'Alice', 'Brown', '+40700123457');

INSERT INTO bookings (user_id, first_name, last_name, phone_number) VALUES ((SELECT user_id FROM users WHERE email = 'manager@transporty.com'), 'Jane', 'Smith', '+40700987654');
INSERT INTO bookings (user_id, first_name, last_name, phone_number) VALUES ((SELECT user_id FROM users WHERE email = 'manager1@transporty.com'), 'Jane', 'Smith', '+40700987654');

-- Introducere date pentru 'booking_details'
INSERT INTO booking_details (booking_id, schedule_id, boarded) VALUES
(1, 1, FALSE),
(1, 2, FALSE),
(2, 3, TRUE),
(3, 4, FALSE),
(3, 5, FALSE);

INSERT INTO booking_details (booking_id, schedule_id, boarded) VALUES (2, 3, TRUE);

-- Introducere date pentru 'booking_segments'
INSERT INTO booking_segments (booking_id, schedule_id, travel_date) VALUES
(1, 1, '2024-06-01'),  -- Valabilă, prima zi disponibilă
(1, 2, '2024-06-08'),  -- Valabilă, conform frecvenței de 7 zile
(2, 3, '2024-06-08'),  -- Valabilă, conform frecvenței de 7 zile
(3, 4, '2024-06-16'),  -- Valabilă, conform frecvenței de 14 zile
(3, 5, '2024-07-03'),  -- Valabilă, conform frecvenței de 1 lună
(3, 6, '2024-07-04');  -- Valabilă, conform frecvenței de 1 lună

CREATE TABLE companies
(
    company_id       integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    company_name     text NOT NULL UNIQUE,
    background_color text NOT NULL,
    foreground_color text NOT NULL
);

CREATE TABLE locations
(
    location_id  integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    full_name    text NOT NULL,
    abbreviation text NOT NULL UNIQUE,
    CONSTRAINT locations_abbreviation_must_be_exactly_3_capital_letters
        CHECK (abbreviation ~* '^[A-Z]{3}$')
);

CREATE TABLE users
(
    user_id    integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    email      text UNIQUE NOT NULL,
    role       text        NOT NULL,
    company_id integer     NOT NULL,
    full_name  text        NOT NULL,
    FOREIGN KEY (company_id) REFERENCES companies ON DELETE CASCADE,
    CONSTRAINT users_role_chk CHECK (
        role IN ('travel_agent_role', 'transportation_company_role', 'boarding_agent_role')
        ),
    CONSTRAINT users_email_chk CHECK (email ~* '[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}')
);

-- Crearea tabelului TravelRoutes
CREATE TABLE travel_routes
(
    route_id              integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    departure_location_id integer NOT NULL,
    arrival_location_id   integer NOT NULL,
    transport_type        text    NOT NULL,
    FOREIGN KEY (departure_location_id) REFERENCES locations ON DELETE RESTRICT,
    FOREIGN KEY (arrival_location_id) REFERENCES locations ON DELETE RESTRICT,
    CONSTRAINT travel_routes_transport_type_known CHECK (transport_type IN ('airplane', 'train', 'ship', 'bus'))
);

-- Crearea tabelului RouteSchedules
CREATE TABLE route_schedules
(
    schedule_id    int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    route_id       int                      NOT NULL,
    departure_time timestamp                NOT NULL,
    arrival_time   timestamp                NOT NULL,
    price          int                      NOT NULL,
    frequency      interval                 NOT NULL,
    valid_from     timestamp WITH TIME ZONE NOT NULL,
    valid_until    timestamp WITH TIME ZONE NOT NULL,
    FOREIGN KEY (route_id) REFERENCES travel_routes ON DELETE CASCADE,
    CONSTRAINT route_schedules_frequency_predetermined_amount CHECK (
        frequency IN ('1 day'::interval, '7 days'::interval, '14 days'::interval, '1 month'::interval)
        )
);

-- Crearea tabelului Bookings
CREATE TABLE bookings
(
    booking_id   integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id      integer NOT NULL,
    first_name   text    NOT NULL,
    last_name    text    NOT NULL,
    phone_number text    NOT NULL,
    FOREIGN KEY (user_id) REFERENCES users ON DELETE CASCADE,
    CONSTRAINT bookings_phone_number_valid CHECK (phone_number ~* '^\+?[0-9]{10,15}$')
);

-- Crearea tabelului BookingDetails
CREATE TABLE booking_details
(
    booking_detail_id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    booking_id        integer NOT NULL,
    schedule_id       integer NOT NULL,
    boarded           boolean NOT NULL DEFAULT FALSE,
    FOREIGN KEY (booking_id) REFERENCES bookings ON DELETE CASCADE,
    FOREIGN KEY (schedule_id) REFERENCES route_schedules ON DELETE CASCADE
);

CREATE TABLE booking_segments
(
    booking_id  integer NOT NULL,
    schedule_id integer NOT NULL,
    travel_date date    NOT NULL,
    FOREIGN KEY (booking_id) REFERENCES bookings ON DELETE CASCADE,
    FOREIGN KEY (schedule_id) REFERENCES route_schedules ON DELETE CASCADE,
    CONSTRAINT booking_segments_unq UNIQUE (booking_id, schedule_id, travel_date)
);

-- Activare RLS pe tabelele necesare
ALTER TABLE companies ENABLE ROW LEVEL SECURITY;
ALTER TABLE locations ENABLE ROW LEVEL SECURITY;
ALTER TABLE travel_routes ENABLE ROW LEVEL SECURITY;
ALTER TABLE route_schedules ENABLE ROW LEVEL SECURITY;
ALTER TABLE bookings ENABLE ROW LEVEL SECURITY;
ALTER TABLE booking_details ENABLE ROW LEVEL SECURITY;
ALTER TABLE booking_segments ENABLE ROW LEVEL SECURITY;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;

-- Crearea politicilor RLS

-- Companies & Users: Permiterea accesului pentru toate rolurile specificate
CREATE POLICY access_companies_for_all ON companies
    USING (
        pg_has_role('travel_agent_role', 'member') OR
        pg_has_role('transportation_company_role', 'member') OR
        pg_has_role('boarding_agent_role', 'member')
    );

CREATE POLICY access_users_for_all ON users
    USING (
        pg_has_role('travel_agent_role', 'member') OR
        pg_has_role('transportation_company_role', 'member') OR
        pg_has_role('boarding_agent_role', 'member')
    );

-- Locations & TravelRoutes: Accesibil tuturor utilizatorilor
CREATE POLICY access_all_locations ON locations USING (true);
CREATE POLICY access_all_travel_routes ON travel_routes USING (true);

-- RouteSchedules, Bookings, BookingDetails, BookingSegments: Acces controlat pe baza user_id și/sau company_id asociat cu user_id curent
CREATE POLICY access_route_schedules ON route_schedules
    USING (
        EXISTS (
            SELECT 1 FROM users u
            WHERE u.user_id = (SELECT user_id FROM users WHERE email = current_user) AND
                  u.company_id = (SELECT company_id FROM travel_routes tr WHERE tr.route_id = route_schedules.route_id)
        )
    );

CREATE POLICY access_bookings ON bookings
    USING (
        bookings.user_id = (SELECT user_id FROM users WHERE email = current_user)
    );

-- Adugarea daca se doreste in tabela bookings a coloanei company_id
ALTER TABLE bookings ADD COLUMN company_id INTEGER;
UPDATE bookings b SET company_id = (SELECT company_id FROM users WHERE user_id = b.user_id);

-- Incercari de politici pentru tabela bookings
CREATE POLICY access_bookings ON bookings
    FOR SELECT USING (
        (SELECT company_id FROM users WHERE email = current_user) = bookings.company_id
    );

CREATE POLICY access_bookings_select ON bookings
    FOR SELECT USING (
        bookings.company_id = (SELECT company_id FROM users WHERE email = current_user)
    );

CREATE POLICY access_bookings_insert ON bookings
    FOR INSERT WITH CHECK (
        pg_has_role('travel_agent_role', 'member') -- Evident, trebuie si restul de rolori cu OR dar era pentru test asta mai mult ca oricum nu merge 
    );

CREATE POLICY access_booking_details ON booking_details
    USING (
        EXISTS (
            SELECT 1 FROM bookings b
            WHERE b.booking_id = booking_details.booking_id AND
                  b.user_id = (SELECT user_id FROM users WHERE email = current_user)
        )
    );

CREATE POLICY access_booking_segments ON booking_segments
    USING (
        EXISTS (
            SELECT 1 FROM bookings b
            WHERE b.booking_id = booking_segments.booking_id AND
                  b.user_id = (SELECT user_id FROM users WHERE email = current_user)
        )
    );



-- Functia pentru crearea unei companii noi
CREATE OR REPLACE PROCEDURE create_company(name text, bg_color text, fg_color text) AS
$$
BEGIN
    INSERT INTO companies (company_name, background_color, foreground_color) VALUES (name, bg_color, fg_color);
    -- Creare rol pentru companie
    EXECUTE 'CREATE ROLE ' || QUOTE_IDENT(name || '_role');
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE create_user(user_email text, password text, comp_name text, role_name text,
                                        user_full_name text) AS
$$
DECLARE
    local_company_id integer;
BEGIN
    SELECT company_id INTO local_company_id FROM companies WHERE company_name = comp_name;
    IF local_company_id IS NULL THEN
        RAISE EXCEPTION 'Company not found';
    END IF;

    -- Creare rol pentru utilizator cu parola asociata
    EXECUTE FORMAT('CREATE ROLE %I LOGIN PASSWORD %L IN ROLE %I, %I', user_email, password, role_name,
                   comp_name || '_role');

    -- Adaugare utilizator in tabela
    INSERT INTO users (email, role, company_id, full_name)
    VALUES (user_email, role_name, local_company_id, user_full_name);
END;
$$ LANGUAGE plpgsql;



CREATE OR REPLACE FUNCTION is_valid_travel_date(_schedule_id int, _travel_date date) RETURNS boolean AS
$$
DECLARE
    v_start_date timestamp WITH TIME ZONE;
    v_frequency  interval;
    v_valid      boolean := FALSE;
BEGIN
    -- Obținerea datelor de start și frecvenței pentru orar
    SELECT valid_from, frequency
    INTO v_start_date, v_frequency
    FROM route_schedules
    WHERE schedule_id = _schedule_id;

    -- Verificarea dacă data călătoriei se încadrează în frecvența orarului
    WHILE v_start_date::date <= _travel_date
        LOOP
            IF v_start_date::date = _travel_date THEN
                v_valid := TRUE;
                EXIT;
            END IF;
            v_start_date := v_start_date + v_frequency;
        END LOOP;

    RETURN v_valid;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION check_travel_date() RETURNS trigger AS
$$
BEGIN
    IF NOT is_valid_travel_date(new.schedule_id, new.travel_date) THEN
        RAISE EXCEPTION 'Data călătoriei nu este validă.';
    END IF;
    RETURN new;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_check_travel_date
    BEFORE INSERT OR UPDATE
    ON booking_segments
    FOR EACH ROW
EXECUTE FUNCTION check_travel_date();

-- Funcție pentru a verifica data călătoriei împotriva orarului valabil
CREATE OR REPLACE FUNCTION validate_travel_date()
    RETURNS trigger AS
$$
DECLARE
    v_schedule record;
BEGIN
    -- Extrage orarul corespunzător schedule_id din NEW
    SELECT valid_from, valid_until
    INTO v_schedule
    FROM route_schedules
    WHERE schedule_id = new.schedule_id;

    -- Verifică dacă travel_date este în intervalul valid
    IF new.travel_date < v_schedule.valid_from OR new.travel_date > v_schedule.valid_until THEN
        RAISE EXCEPTION 'Data călătoriei % este în afara intervalului valid (% - %)', new.travel_date, v_schedule.valid_from, v_schedule.valid_until;
    END IF;

    RETURN new;
END;
$$ LANGUAGE plpgsql;

-- Crearea trigger-ului pentru a valida data înainte de inserarea sau actualizarea în booking_segments
CREATE TRIGGER validate_booking_segment_travel_date
    BEFORE INSERT OR UPDATE
    ON booking_segments
    FOR EACH ROW
EXECUTE FUNCTION validate_travel_date();

-- Crearea view-urilor cu optiuni de securitate
CREATE OR REPLACE VIEW travel_agent_view WITH (security_barrier = true) AS
SELECT b.booking_id, b.user_id, b.first_name, b.last_name, b.phone_number, bd.boarded
FROM bookings b
         JOIN booking_details bd ON b.booking_id = bd.booking_id
         JOIN users u ON b.user_id = u.user_id
WHERE u.role = 'travel_agent_role';

CREATE OR REPLACE VIEW transportation_company_view WITH (security_barrier = true) AS
SELECT r.route_id, rs.departure_time, rs.arrival_time, rs.price, rs.frequency, rs.valid_from, rs.valid_until
FROM travel_routes r
         JOIN route_schedules rs ON r.route_id = rs.route_id
         JOIN users u ON r.route_id = u.company_id
WHERE u.role = 'transportation_company_role';

CREATE OR REPLACE VIEW boarding_agent_view WITH (security_barrier = true) AS
SELECT bd.booking_detail_id, bd.booking_id, bd.schedule_id, bd.boarded
FROM booking_details bd
         JOIN users u ON bd.schedule_id = u.company_id
WHERE u.role = 'boarding_agent_role'
  AND bd.boarded = FALSE;

-- Crearea rolurilor functionale
CREATE ROLE travel_agent_role;
CREATE ROLE transportation_company_role;
CREATE ROLE boarding_agent_role;

-- Acordarea de permisiuni pe vederi
GRANT SELECT ON travel_agent_view TO travel_agent_role;
GRANT SELECT ON transportation_company_view TO transportation_company_role;
GRANT SELECT ON boarding_agent_view TO boarding_agent_role;

GRANT SELECT ON users TO transportation_company_role;
GRANT SELECT ON companies TO transportation_company_role;
GRANT SELECT ON locations TO transportation_company_role;
GRANT SELECT ON companies TO transportation_company_role;
GRANT SELECT ON travel_routes to transportation_company_role;
GRANT SELECT ON route_schedules to transportation_company_role;
GRANT INSERT ON travel_routes to transportation_company_role;
GRANT INSERT ON route_schedules to transportation_company_role;
GRANT DELETE ON route_schedules TO transportation_company_role;
GRANT UPDATE ON route_schedules TO transportation_company_role;

GRANT SELECT ON users TO travel_agent_role;
GRANT SELECT ON locations TO travel_agent_role;
GRANT SELECT ON travel_routes TO travel_agent_role;
GRANT SELECT ON route_schedules TO travel_agent_role;
GRANT SELECT ON companies TO travel_agent_role;
GRANT SELECT ON bookings to travel_agent_role;
GRANT INSERT ON bookings to travel_agent_role;
GRANT DELETE ON bookings TO travel_agent_role;
GRANT SELECT ON booking_details to travel_agent_role;
GRANT INSERT ON booking_details TO travel_agent_role;
GRANT DELETE ON booking_details TO travel_agent_role;

GRANT SELECT ON users TO boarding_agent_role;
GRANT SELECT ON locations to boarding_agent_role;
GRANT SELECT ON route_schedules to boarding_agent_role;
GRANT SELECT ON travel_routes TO boarding_agent_role;
GRANT SELECT ON bookings to boarding_agent_role;
GRANT SELECT ON booking_details to boarding_agent_role;

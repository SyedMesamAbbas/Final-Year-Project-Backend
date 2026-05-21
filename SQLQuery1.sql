Create Database HouseofTutor
use HouseofTutor

CREATE TABLE Users (
    user_id INT PRIMARY KEY IDENTITY(1,1),
    full_name VARCHAR(100),
    email VARCHAR(100) UNIQUE,
    phone VARCHAR(20),
    cnic VARCHAR(20),
    password VARCHAR(100),
    role VARCHAR(20) CHECK (role IN ('Student', 'Tutor', 'Admin'))
);

CREATE TABLE Tutor (
    tutor_id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    qualification VARCHAR(100),
    experience INT,
    location VARCHAR(Max),
    radius INT,
    status VARCHAR(20),
    Latitude FLOAT, 
    Longitude FLOAT,

    FOREIGN KEY (user_id) REFERENCES Users(user_id)
);


CREATE TABLE Student (
    student_id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    location VARCHAR(Max),
    Latitude FLOAT, 
    Longitude FLOAT,

    FOREIGN KEY (user_id) REFERENCES Users(user_id)
);


CREATE TABLE Course (
    course_id INT PRIMARY KEY IDENTITY(1,1),
    course_title VARCHAR(100)
);


CREATE TABLE Schedule (
    schedule_id INT PRIMARY KEY IDENTITY(1,1),
    tutor_id INT,
    day VARCHAR(20),
    time VARCHAR(100),
    start_date DATE,
    end_date DATE,
    Type varchar(50),

    FOREIGN KEY (tutor_id) REFERENCES Tutor(tutor_id)
);


CREATE TABLE Student_Schedule (
    schedule_id INT PRIMARY KEY IDENTITY(1,1),
    student_id INT,
    day VARCHAR(20),
    time VARCHAR(100),
    start_date DATE,
    end_date DATE,
    Type varchar(50),

    FOREIGN KEY (student_id) REFERENCES Student(student_id),
);

CREATE TABLE Tutor_Course (
    tutor_id INT,
    course_id INT,
    grade VARCHAR(10),

    PRIMARY KEY (tutor_id, course_id),
    FOREIGN KEY (tutor_id) REFERENCES Tutor(tutor_id),
    FOREIGN KEY (course_id) REFERENCES Course(course_id)
);

DROP TABLE Student_Course;

CREATE TABLE Student_Course (
    student_id INT,
    course_id INT,

    PRIMARY KEY (student_id, course_id),
    FOREIGN KEY (course_id) REFERENCES Course(course_id)
);


CREATE TABLE Request (
    request_id INT PRIMARY KEY IDENTITY(1,1),
    student_id INT,
    tutor_id INT,
    course_id INT,
    request_date DATETIME DEFAULT GETDATE(),
    Status Varchar(50),
    Time VARCHAR(100),

    FOREIGN KEY (student_id) REFERENCES Student(student_id),
    FOREIGN KEY (tutor_id) REFERENCES Tutor(tutor_id),
    FOREIGN KEY (course_id) REFERENCES Course(course_id)
);


CREATE TABLE Feedback (
    feedback_id INT PRIMARY KEY IDENTITY(1,1),
    student_id INT,
    tutor_id INT,
    course_id INT,
    rating INT CHECK (rating BETWEEN 1 AND 5),
    comment VARCHAR(255),
    feedback_date DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (student_id) REFERENCES Student(student_id),
    FOREIGN KEY (tutor_id) REFERENCES Tutor(tutor_id),
    FOREIGN KEY (course_id) REFERENCES Course(course_id)
);

Alter table Schedule Add start_date DATE;
Alter table Schedule Add end_date DATE;
Alter Table Schedule Add Type varchar(50);

Alter table Student_Schedule Add start_date DATE;
Alter table Student_Schedule Add end_date DATE;
Alter Table Student_Schedule Add Type varchar(50);

Select * from Users
Select * from Student
Select * from Tutor
Select * from Course
Select * from Schedule
Select * from Student_Schedule
Select * from Tutor_Course
Select * from Student_Course
Select * from Request
Select * from Feedback
Delete from Schedule where schedule_id>=1325

Update Request set Status ='Pending' where tutor_id=2

INSERT INTO Users (full_name, email, phone, cnic, password, role)
VALUES ('System Administrator', 'admin@example.com', '033123456789', '12345-6789012-3', 'SecurePass123!', 'Tutor');



INSERT INTO Users (full_name, email, phone, cnic, password, role)

VALUES ('Student', 'student@example.com', '033123456789', '12345-6789012-2', 'SecurePass123!', 'Student');
INSERT INTO Users (full_name, email, phone, cnic, password, role)
VALUES ('Tutor', 'Tutor@example.com', '033123456789', '12345-6789012-2', 'SecurePass123!', 'Tutor');

Insert into Users (full_name, email, phone, cnic, password, role) Values
('ABC','abc123@gmail.com','03331223344','12345-6789012-5','123','Admin');

INSERT INTO Schedule (tutor_id, day, time)
VALUES (6, 'Tuesday', '11:00-12:00pm');

Insert into Tutor (user_id,qualification ,experience ,location ,radius,status)values
(9, 'MBBS',3,'null',20,'Active');


Alter table Request Add Time Varchar(50)
Update Schedule set time ='' where tutor_id=2

INSERT INTO Schedule (tutor_id, day, time)
VALUES (2, 'Monday', '10:00-11:00am');
INSERT INTO Schedule (tutor_id, day, time)
VALUES (3, 'Monday', '10:00-11:00am');
INSERT INTO Schedule (tutor_id, day, time)
VALUES (8, 'Tuesday', '11:00-12:00pm');

Select * from Users
Select * from Student
Select * from Tutor
Select * from Schedule
Select * from Course
Select * from Request
Select * from Feedback
Select * from Tutor_Course
insert into Tutor_Course(tutor_id, course_id, grade)Values (6,1,'A');

Update Request set Time ='12:00-1:00pm' where request_id =7
-- Record 1: Student 'Syed Mesam Abbas' (ID 5) requests 'Usaid Ur Rehman' (ID 2)
INSERT INTO Request (student_id, tutor_id, course_id)
VALUES (12, 1, 1);

-- Record 2: Student 'Maryam bibi' (ID 7) requests 'Faizan shahid' (ID 6)
INSERT INTO Request (student_id, tutor_id, course_id)
VALUES (2, 2, 1);

-- Record 3: Student 'Manna Rana' (ID 8) requests 'Tutor' (ID 9)
INSERT INTO Request (student_id, tutor_id, course_id)
VALUES (3, 3, 2);


INSERT INTO Course (course_title)
VALUES 
('Introduction to Computer Science'),
('Calculus I'),
('Data Structures and Algorithms'),
('Database Management Systems'),
('Web Development');


Update Student set Latitude=33.6844 where student_id=1
Update Student set Longitude=73.0479 where student_id=1

Delete Request where request_id=4
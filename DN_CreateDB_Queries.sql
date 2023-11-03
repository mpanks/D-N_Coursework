-- INSERT forename(userID, name)  VALUES('717402','Matthew');
CREATE DATABASE whois;

USE whois;

CREATE TABLE users
(
    userID       VARCHAR(255) NOT NULL,
    forenames    VARCHAR(255) NOT NULL,
    surname      VARCHAR(255),
    title        VARCHAR(255) NOT NULL,
    position     VARCHAR(255) NOT NULL,
    userLocation VARCHAR(255) NOT NULL,
    CONSTRAINT PK_User PRIMARY KEY (userID)
);

CREATE TABLE phoneNumber
(
    userID VARCHAR(255) NOT NULL,
    phone  VARCHAR(20)  NOT NULL,
    CONSTRAINT PK_PhoneNumber PRIMARY KEY (userID, phone),
    CONSTRAINT FK_PhoneNumber FOREIGN KEY (userID) REFERENCES users (userID)
);

CREATE TABLE emails
(
    emailID INT AUTO_INCREMENT NOT NULL,
    email   VARCHAR(255) NOT NULL,
    CONSTRAINT PK_Email PRIMARY KEY (emailID)
);

CREATE TABLE usersEmail
(
    userID  VARCHAR(255) NOT NULL,
    emailID INT NOT NULL,
    CONSTRAINT PK_UsersEmail PRIMARY KEY (userID, emailID),
    CONSTRAINT FK_UserID FOREIGN KEY (userID) REFERENCES users (userID),
    CONSTRAINT FK_EmailID FOREIGN KEY (emailID) REFERENCES emails (emailID)
);

CREATE TABLE loginDetails
(
    userID  VARCHAR(255) NOT NULL,
    loginID VARCHAR(50)  NOT NULL,
    CONSTRAINT PK_LoginDetails PRIMARY KEY (userID),
    CONSTRAINT FK_loginDetails FOREIGN KEY (userID) REFERENCES users (userID)
);

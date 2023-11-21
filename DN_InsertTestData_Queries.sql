
-- Add fields to users first due to dependencies
INSERT INTO users VALUES('717402','Matthew Herbert','Panks','Mr','Student','InTheLab');
INSERT INTO users VALUES('789214','John','Smith','Mr','Student','RBB-209');
INSERT INTO users VALUES('978456','Ash','Entwisle','Miss','Student','InTheLab');
INSERT INTO users VALUES('645798','Brian','Tompsett','Eur Ing','Professor','InTheLab');
INSERT INTO users VALUES('451236','Bing','Wang','Dr','Professor','RBB-209');
INSERT INTO users VALUES('124574','Charles Phillip Arthur George',' ','His Royal Highness','King of England','Buckingham Palace');
INSERT INTO users VALUES('465779', 'Ada','Lovelace','Mrs','Mathematician and Writer','Church of St Mary Magdelene');
INSERT INTO users VALUES('457845','Charles','Babbage','Father of the Computer','Polymath and Mathematician','Kensal Green Cemetary');
Insert INTO users VALUES('325678','Alan','Turing','Prof','Mathematician and Computer Scientist','Woking crematorium');
INSERT INTO users VALUES('124578','Steve','Wozniack','Mr','Apple co Founder','Allam Lecture Theatre');

INSERT INTO logindetails VALUES('717402','123456');
INSERT INTO logindetails VALUES('789214','654321');
INSERT INTO logindetails VALUES('978456','789456');
INSERT INTO logindetails VALUES('645798','cssbct');
INSERT INTO logindetails VALUES('451236','cssbct');
INSERT INTO logindetails VALUES('124574','Majesty');
INSERT INTO logindetails VALUES('465779','Analytical');
INSERT INTO logindetails VALUES('457845','Analytical');
INSERT INTO logindetails VALUES('325678','Enigma');
INSERT INTO logindetails VALUES('124578','Macintosh');

INSERT INTO phonenumber VALUES('717402','07936129686');
INSERT INTO phonenumber VALUES('789214','01405839173');
INSERT INTO phonenumber VALUES('978456','01405235124');
INSERT INTO phonenumber VALUES('645798','07451235784');
INSERT INTO phonenumber VALUES('451236','07451235784');
INSERT INTO phonenumber VALUES('124574','07845112457');
INSERT INTO phonenumber VALUES('465779','0800001066');
INSERT INTO phonenumber VALUES('457845','0800001066');
INSERT INTO phonenumber VALUES('325678','01405784512');
INSERT INTO phonenumber VALUES('124578','07451245745');

INSERT INTO emails(email) VALUES('m.panks-2021@hull.ac.uk');
INSERT INTO usersemail VALUES('717402', (SELECT last_insert_id()));

INSERT INTO emails(email) VALUES('j.smith-2022@hull.ac.uk');
INSERT INTO usersemail VALUES('789214',(SELECT last_insert_id()));

INSERT INTO emails(email) VALUES('a.entwisle@hull.ac.uk');
INSERT INTO usersemail VALUES('978456',(SELECT last_insert_id()));

INSERT INTO emails(email) VALUES('b.Tompsett@hull.ac.uk');
INSERT INTO usersemail VALUES('645798',(SELECT last_insert_id()));

INSERT INTO emails(email) VALUES('b.Wang@hull.ac.uk');
INSERT INTO usersemail VALUES('451236',(SELECT last_insert_id()));

INSERT INTO emails(email) VALUES('TheKing@Monarch.Gov.Org.UK');
INSERT INTO usersemail VALUES('124574',(SELECT last_insert_id()));

INSERT INTO emails(email) VALUES('Ada.LoveLace1815@gmail.com');
INSERT INTO usersemail VALUES('465779',(SELECT last_insert_id()));

INSERT INTO emails(email) VALUES('C.Babbage@plusnet.com');
INSERT INTO usersemail VALUES('457845',(SELECT last_insert_id()));

INSERT INTO emails(email) VALUES('Alan@Secrets.gov.org.uk');
INSERT INTO usersemail VALUES('325678',(SELECT last_insert_id()));

INSERT INTO emails(email) VALUES('TheWoz@Apple.co');
INSERT INTO usersemail VALUES('124578',(SELECT last_insert_id()));

INSERT INTO emails(email) VALUES('ComputerScienceDept@hull.ac.uk');
INSERT INTO usersemail VALUES('645798',(SELECT last_insert_id()));
INSERT INTO usersemail VALUES('451236',(SELECT emailID from emails WHERE email = 'ComputerScienceDept@hull.ac.uk'));

INSERT INTO emails(email) VALUES('Team@AnalyticalEngine.Org');
INSERT INTO usersemail VALUES('465779',(SELECT last_insert_id()));
INSERT INTO usersemail VALUES('457845',(SELECT emailID from emails WHERE email = 'Team@AnalyticalEngine.Org'));

SELECT Users.userID, forenames, surname, title, position, userLocation, email, loginID, phone
FROM users, logindetails, phonenumber, emails, usersemail
WHERE users.userID = logindetails.userID
AND users.userID = phonenumber.userID
AND users.userID = usersemail.userID
AND usersemail.emailID = emails.emailID;
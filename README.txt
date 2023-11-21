Location of files and folders:

Creating database queries: DN_CreateDB_Queries
Add test data: DN_InsertTestDataDB_Queries

Code: Program.cs
whois.exe: can be found in whois->bin->debug->net6.0, the file is near the bottom of the folder

ERD: DN_ERD_PDF

Special Instructions:

Execution of the program is as described in the labs and assignment insutrctuins, opening the whois.exe file location in command prompt then entering "whois" followed by the command (or commands) for the static client, or simply entering "whois" without arguments to start the server, which will listen to port 43.

The static client can handle commands to operate on any field in the database, these are not case sensitive for the satic client or server, however; the server can only handle one HTTP request at a time with one command each

While in the database the field 'location' is named 'userLocation', inputting either to the static client or server will yield the same result and both work appropriately. The same is true for the stored phonenumber which is stored as 'phone' in the database but 'phone' and 'phonenumber' are interchangable in commands

The syntax described in the labs has been used for both the static client and server

For this submission the users loginID has been used for command operations (i.e. csssbct?=location, where cssbct is the loginID), not the userID (as per the majority of the labs and assignment instructions)

Besides that, the software has been made to the specification of the labs and assignment instructions
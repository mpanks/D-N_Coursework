namespace whois
{
    using MySql.Data;
    using MySql.Data.MySqlClient;
    using Org.BouncyCastle.Asn1.Utilities;
    using Org.BouncyCastle.Bcpg;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection.Metadata;
    internal class Program
    {
        static bool debug = true; //TODO change debug to false before final commit
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Server");

            if (args.Length == 0)
            {
                RunServer();
            }
            else
            {
                for (int i = 0; i < args.Length; i++)
                {
                    ProcessCommand(args[i]);
                }
            }
        }
        static void ProcessCommand(string command)
        {
            if (debug) Console.WriteLine($"\nCommand: {command}");
            try
            {
                //<loginID>?<field>=<update>
                //operation is <field>=<update>

                //Gets base info in command from console
                String[] slice = command.Split(new char[] { '?' }, 2);
                String ID = slice[0];
                String operation = null;
                String update = null;
                String field = null;
                if (slice.Length == 2)
                {
                    operation = slice[1];
                    String[] pieces = operation.Split(new char[] { '=' }, 2);
                    field = pieces[0];
                    if (pieces.Length == 2) update = pieces[1];
                }

                if (debug) Console.WriteLine($"Operation on ID '{ID}'");
                ServerCommands servCmd = new ServerCommands("localhost", "root", "whois", "3306", "L3tM31n");

                if (operation == null) //Nothing after '?'
                {
                    servCmd.Dump(ID);
                }
                else if (update == null && !servCmd.CheckDBID(ID))  //Cannot find ID in DB, and update field is null
                                                                    //Null updates are caught in update function
                {
                    Console.WriteLine($"User {ID} is unknown");
                }
                else if (operation == "")
                {
                    servCmd.Delete(ID);
                    return;
                }
                else if (!servCmd.CheckDBID(ID))
                {
                    //Add New User
                    servCmd.AddNewUser(ID, field, update);
                }
                else if (update == null)
                {
                    servCmd.Lookup(ID, field);
                }
                else
                {
                    servCmd.Update(ID, field, update);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Fault in Command Processing: " + e.ToString());
            }
        }
        class ServerCommands
        {
            string conStr = "Server=localhost; user=root;" +
            "database=whois;port=3306;password=L3tM31n";

            MySqlConnection conn;
            public ServerCommands(
                String ServerName,
                String User,
                String DatabaseName,
                String PortNum,
                String Password)
            {
                conStr = $"Server={ServerName};user={User};" +
                    $"database={DatabaseName};port={PortNum};password={Password}";
                conn = new MySqlConnection(conStr);
                conn.Open();
                Console.WriteLine("Connection opened");
            }
            public bool CheckDBID(string ID)
            {
                //Checks ID is present in DB
                var cmd = new MySqlCommand();
                cmd.Connection = conn;

                cmd.CommandText = "SELECT * from loginDetails WHERE loginID = @ID;";
                cmd.Parameters.AddWithValue("@ID", ID);
                if (cmd.ExecuteScalar() != null)
                {
                    //ID is present
                    return true;
                }
                else
                {
                    //ID is not present
                    return false;
                }
            }

            public void Delete(String ID)
            {
                //Removes information stored under a given loginID
                if (debug) Console.WriteLine($"Delete record '{ID}' from DataBase");
                var cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "DELETE phonenumber FROM phonenumber " +
                    "WHERE userID = (SELECT userID FROM logindetails WHERE loginID = @ID); " +

                    "DELETE emails, usersemail FROM usersemail INNER JOIN emails ON emails.emailID = usersemail.emailID " +
                    "WHERE usersemail.userID = (SELECT userID FROM loginDetails WHERE loginID = @ID); " +

                    //Login details must be deleted last due to dependencies
                    "DELETE users, logindetails FROM logindetails INNER JOIN users ON logindetails.userID = users.userID " +
                    "WHERE loginID = @ID;";
                cmd.Parameters.AddWithValue("@ID", ID);
                if (cmd.ExecuteNonQuery() != 0)
                {
                    Console.WriteLine($"User with ID {ID} has been deleted");
                }
                else { Console.WriteLine($"Unable to remove user {ID}"); } //Should never happen
            }
            public string Dump(string ID)
            {
                //Outputs all information stored under a given loginID
                //ID is LoginID - needs to dump every person who has that LoginID

                MySqlCommand cmd = new MySqlCommand();
                cmd.CommandText = "select users.UserID, Forenames, surname, title, position, userLocation, phone, email " +
                    "FROM users, phonenumber, usersemail, emails, logindetails " +
                    "WHERE users.userID = phonenumber.userID " +
                    "AND users.userID = usersemail.userID " +
                    "AND usersemail.emailID = emails.emailID " +
                    "AND logindetails.userID IN (SELECT userID FROM logindetails " +
                    "WHERE logindetails.loginID = @loginID)" +
                    "AND users.userID = logindetails.userID;";
                //UserID, Forename, Lastname, title, position, userlocation, phonenumber, email

                cmd.Connection = conn;
                cmd.Parameters.Add(new MySqlParameter("@loginID", ID));
                cmd.ExecuteNonQuery();
                string output = "";

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            output += $"{reader.GetName(i)}: {reader.GetString(i)}\n";
                        }
                        output += "\n";
                    }
                    Console.WriteLine(output);
                }
                if (output == "")
                {
                    Console.WriteLine("Cannot find user");
                }
                conn.Close();
                return output;
            }
            public string Lookup(string ID, string field)
            {
                //Lookup request from console
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT * " +
                    " FROM users, logindetails, emails, phonenumber, usersemail " +
                    "WHERE users.userID = logindetails.userID " +
                    "AND logindetails.loginID = @ID " +
                    "AND users.userID IN " +
                    "(SELECT userID FROM loginDetails WHERE loginID = @ID) " +
                    "AND users.userID = usersemail.userID " +
                    "AND usersemail.emailID = emails.emailID " +
                    "AND phonenumber.userID = users.userID;";
                cmd.Parameters.AddWithValue("@ID", ID);

                cmd.ExecuteNonQuery();
                string output = string.Empty;
                if (field.ToLower() == "location" || field.ToLower() == "userlocation") field = "userLocation";
                else if (field.ToLower() == "phone" || field.ToLower() == "phonenumber") field = "phone";

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader.GetName(i).Equals(field) && !output.Contains(reader.GetString(i)))
                            {
                                output += $"{reader.GetName(i)}: {reader.GetString(i)}\n";
                            }
                        }
                    }
                }
                if (output != string.Empty)
                {
                    //Checks user has a stored value in the given field
                    if (output.Split(' ')[1] == "") output = $"{ID} has no listed {field}";

                    Console.WriteLine(output);
                    return output;
                }
                else
                {
                    //Cannot find field
                    Console.WriteLine($"Unknown field {field}");
                    return null;
                }
            }
            public void AddNewUser(string ID, string field, string value)
            {
                //Adds new user to DB
                //generates new userID - not set to AutoIncrement
                Random rnd = new Random();
                string userID = string.Empty;
                do
                {
                    userID = rnd.Next(100000, 999999).ToString();
                    if (debug) Console.WriteLine(userID);
                } while (CheckDBID(userID));
                MySqlCommand cmd = new MySqlCommand();
                var ins = new MySqlCommand();
                ins.Connection = conn;
                cmd.Connection = conn;

                //Creates SQL command based on given field
                //MySQL doesn't support fields being added as parameters
                switch(field.ToLower())
                {
                    case "phonenumber":
                    case "phone":
                        cmd.CommandText = "INSERT INTO users(userID, userLocation, forenames, surname, title, position) VALUES(@userID, ' ', ' ',' ',' ',' '); " +
                        "INSERT INTO emails(email) VALUES(' '); " +
                        "INSERT INTO usersemail(userID, emailID) VALUES( @userID, (SELECT LAST_INSERT_ID()) ); " +
                        "INSERT INTO phonenumber(userID, phone) VALUES(@userID, @update);";
                        break;
                    case "email":
                        cmd.CommandText = "INSERT INTO users(userID, userLocation, forenames, surname, title, position) VALUES(@userID, ' ', ' ',' ',' ',' '); " +
                        "INSERT INTO emails(email) VALUES(@update); " +
                        "INSERT INTO usersemail(userID, emailID) VALUES( @userID, (SELECT LAST_INSERT_ID()) ); " +
                        "INSERT INTO phonenumber(userID, phone) VALUES(@userID, ' ');";
                        break;
                    case "location":
                    case "userlocation":
                        cmd.CommandText = "INSERT INTO users(userID, userLocation, forenames, surname, title, position) VALUES(@userID, @update, ' ',' ',' ',' '); " +
                        "INSERT INTO emails(email) VALUES(' '); " +
                        "INSERT INTO usersemail(userID, emailID) VALUES( @userID, (SELECT LAST_INSERT_ID()) ); " +
                        "INSERT INTO phonenumber(userID, phone) VALUES(@userID, ' ');";
                        break;
                    case "forenames":
                        cmd.CommandText = "INSERT INTO users(userID, userLocation, forenames, surname, title, position) VALUES(@userID, ' ', @update,' ',' ',' '); " +
                        "INSERT INTO emails(email) VALUES(' '); " +
                        "INSERT INTO usersemail(userID, emailID) VALUES( @userID, (SELECT LAST_INSERT_ID()) ); " +
                        "INSERT INTO phonenumber(userID, phone) VALUES(@userID, ' ');";
                        break;
                    case "surname":
                        cmd.CommandText = "INSERT INTO users(userID, userLocation, forenames, surname, title, position) VALUES(@userID, ' ', ' ',@update,' ',' '); " +
                        "INSERT INTO emails(email) VALUES(' '); " +
                        "INSERT INTO usersemail(userID, emailID) VALUES( @userID, (SELECT LAST_INSERT_ID()) ); " +
                        "INSERT INTO phonenumber(userID, phone) VALUES(@userID, ' ');";
                        break;
                    case "title":
                        cmd.CommandText = "INSERT INTO users(userID, userLocation, forenames, surname, title, position) VALUES(@userID, ' ', ' ',' ',@update,' '); " +
                        "INSERT INTO emails(email) VALUES(' '); " +
                        "INSERT INTO usersemail(userID, emailID) VALUES( @userID, (SELECT LAST_INSERT_ID()) ); " +
                        "INSERT INTO phonenumber(userID, phone) VALUES(@userID, ' ');";
                        break;
                    case "position":
                        cmd.CommandText = "INSERT INTO users(userID, userLocation, forenames, surname, title, position) VALUES(@userID, ' ', ' ',' ',' ',@update); " +
                        "INSERT INTO emails(email) VALUES(' '); " +
                        "INSERT INTO usersemail(userID, emailID) VALUES( @userID, (SELECT LAST_INSERT_ID()) ); " +
                        "INSERT INTO phonenumber(userID, phone) VALUES(@userID, ' ');";
                        break;
                    default:
                        Console.WriteLine($"Unrecognised field {field}");
                        return;
                }
                cmd.Parameters.AddWithValue("@update", value);
                cmd.Parameters.AddWithValue("@userID", userID);

                ins.CommandText = "INSERT INTO loginDetails(userID, loginID) VALUES (@userID, @loginID);";
                ins.Parameters.AddWithValue("@userID", userID);
                ins.Parameters.AddWithValue("@loginID", ID);
                cmd.ExecuteNonQuery();
                ins.ExecuteNonQuery();
                conn.Close();

                Console.WriteLine("Added new user");
            }
            public string Update(string ID, string field, string update)
            {
                //Updates given field with given loginID and update info
                MySqlCommand cmd = new MySqlCommand();

                //Set command text based on given field
                switch (field.ToLower())
                {
                    case "userlocation":
                    case "location":
                        cmd.CommandText = "UPDATE Users SET userLocation = @update WHERE userID IN " +
                            "(SELECT userID FROM logindetails WHERE loginID = @ID);";
                        break;
                    case "forename":
                        cmd.CommandText = "UPDATE Users SET forenames = @update WHERE userID IN " +
                        "(SELECT userID FROM logindetails WHERE loginID = @ID);";
                        break;
                    case "lastname":
                        cmd.CommandText = "UPDATE Users SET surname = @update WHERE userID IN " +
                        "(SELECT userID FROM logindetails WHERE loginID = @ID);";
                        break;
                    case "title":
                        cmd.CommandText = "UPDATE Users SET title = @update WHERE userID IN " +
                        "(SELECT userID FROM logindetails WHERE loginID = @ID);";
                        break;
                    case "position":
                        cmd.CommandText = "UPDATE Users SET position = @update WHERE userID IN " +
                        "(SELECT userID FROM logindetails WHERE loginID = @ID);";
                        break;
                    case "phonenumber":
                    case "phone":
                        cmd.CommandText = "UPDATE phonenumber, users SET phone = @update WHERE users.userID IN " +
                            "(SELECT userID FROM loginDetails WHERE loginID = @ID) " +
                            "AND phonenumber.userID = users.userID;";
                        break;
                    case "email":
                        cmd.CommandText = "UPDATE emails SET email = @update WHERE emailID IN " +
                            "(SELECT emailID FROM usersEmail WHERE userID IN " +
                            "(SELECT userID FROM loginDetails WHERE loginID = @ID));";
                        break;
                    default:
                        Console.WriteLine($"Unkown field {field}");
                        return $"Unknown field {field}";

                }
                if(update==string.Empty)
                {
                    Console.WriteLine($"Cannot update {field} to null");
                    return $"Cannot update {field} to null";
                }
                cmd.Parameters.AddWithValue("@update", update);
                cmd.Parameters.AddWithValue("@ID", ID);

                cmd.Connection = conn;
                if (cmd.ExecuteNonQuery() >= 1)
                {
                    //Checks if fields have been affected
                    Console.WriteLine($"Updated {field} to {update} for {ID}");
                    conn.Close();
                    return $"Updated {field} to {update} for {ID}";
                }
                else
                {
                    //No fields affected
                    Console.WriteLine($"Cannot update user {ID}, unknown field {field}");
                    return $"Unknown field {field}";
                }
            }
        }
        static void RunServer()
        {
            //Args array is empty - starts server
            TcpListener listener;
            Socket connection;
            NetworkStream socketStream;
            try
            {
                listener = new TcpListener(43);
                while (true)
                {
                    if (debug) Console.WriteLine("Server Waiting connection...");
                    listener.Start();
                    connection = listener.AcceptSocket();
                    connection.SendTimeout = 1000;
                    connection.ReceiveTimeout = 1000;
                    socketStream = new NetworkStream(connection);
                    doRequest(socketStream);
                    socketStream.Close();
                    connection.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            if (debug)
                Console.WriteLine("Terminating Server");
        }
        static void doRequest(NetworkStream socketStream)
        {
            //Does request received from webpage
            ServerCommands sc = new ServerCommands("localhost", "root", "whois", "3306", "L3tM31n");
            StreamWriter sw = new StreamWriter(socketStream);
            StreamReader sr = new StreamReader(socketStream);

            if (debug) Console.WriteLine("Waiting for input from client...");
            try
            {
                String line = sr.ReadLine();
                Console.WriteLine($"Received Network Command: '{line}'");

                if (line == null)
                {
                    if (debug) Console.WriteLine("Ignoring null command");
                    return;
                }
                if (line.StartsWith("POST ") && line.EndsWith("HTTP/1.1"))
                {
                    // The we have an update
                    if (debug) Console.WriteLine("Received an update request");
                    int content_length = 0;

                    while (line != "")
                    {
                        if (line.StartsWith("Content-Length: "))
                        {
                            content_length = Int32.Parse(line.Substring(16));
                        }
                        line = sr.ReadLine();
                        if (debug) Console.WriteLine($"Skipped Header Line: '{line}'");
                    }

                    if (debug) Console.WriteLine("Line: " + line);
                    line = "";
                    for (int i = 0; i < content_length; i++) line += (char)sr.Read();
                    string[] userID = new string[2];
                    string[] update = new string[2];

                    String[] slices = line.Split(new char[] { '&' }, 2);
                    try
                    {
                        userID = slices[0].Split(new char[] { '=' }, 2);
                        update = slices[1].Split(new char[] { '=' }, 2);

                    }
                    catch (Exception e)
                    {
                        // This is an invalid request
                        sw.WriteLine("HTTP/1.1 400 Bad Request");
                        sw.WriteLine("Content-Type: text/plain");
                        sw.WriteLine();
                        sw.WriteLine($"Unrecognised command {line}");
                        sw.Flush();
                        if(debug) Console.WriteLine($"Unrecognised command: '{line}' " +e.ToString());
                        return;
                    }
                    ServerCommands servCom = new ServerCommands("localhost", "root", "whois", "3306", "L3tM31n");
                    string output = servCom.Update(userID[1], update[0], update[1]);
                    if (output.Contains("Unknown"))
                    {
                        sw.WriteLine("HTTP/1.1 400 Bad Request");
                        sw.WriteLine("Content-Type: text/plain");
                        sw.WriteLine();
                        sw.WriteLine(output);
                        sw.Flush();
                    }
                    else
                    {
                        sw.WriteLine("HTTP/1.1 200 OK");
                        sw.WriteLine("Content-Type: text/plain");
                        sw.WriteLine();
                        sw.WriteLine(output);
                        sw.Flush();
                    }
                }
                else if (line.StartsWith("GET") && line.EndsWith("HTTP/1.1"))
                {
                    // then we have a lookup
                    if (debug) Console.WriteLine("Received a lookup request");
                    String[] slices = new string[3];
                    String[] command = new String[2];
                    try
                    {
                        slices = line.Split(" ");  // Split into 3 pieces
                        command = slices[1].Split("=", 2);

                        sw.WriteLine("HTTP/1.1 200 OK");
                        sw.WriteLine("Content-Type: text/plain");
                        sw.WriteLine();//Blank line IS IMPORTANT
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Unrecognised command or ID in {line}");
                        sw.WriteLine("HTTP/1.1 400 Bad Request");
                        sw.WriteLine("Content-Type: text/plain");
                        sw.WriteLine();
                    }

                    string output = sc.Lookup(command[1], "userLocation");
                    if (output != null)
                    {
                        sw.WriteLine($"Lookup performed on ID: {command[1]}");
                        sw.WriteLine($"Result: {output}");
                    }
                    else
                    {
                        sw.WriteLine("User Not Found");
                    }
                    sw.Flush();

                    if (debug) Console.WriteLine(command[1]);
                    if (debug) Console.WriteLine(line);
                }
                else
                {
                    // We have an error
                    sw.WriteLine("HTTP/1.1 400 Bad Request");
                    sw.WriteLine("Content-Type: text/plain");
                    sw.WriteLine();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Fault in Command Processing: " + e.ToString());
            }
            finally
            {
                sw.Close();
                sr.Close();
            }
        }
    }
}
namespace whois
{
    using MySql.Data;
    using MySql.Data.MySqlClient;
    using Org.BouncyCastle.Asn1.Utilities;
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Reflection.Metadata;
    using System.Security.Cryptography;

    //TODO Set SQL Statements to work with the up-to-date 3rd normalised DB
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
                if (operation == null)
                {
                    servCmd.Dump(ID);
                }
                else if ((operation == null || update == null) && !servCmd.CheckDBID(ID))
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
            //TODO set password to L3tM31n before submission
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
                this.Output("Connection opened");
            }
            public bool CheckDBID(string ID)
            {
                //Checks ID is present in DB
                var cmd = new MySqlCommand();
                cmd.Connection = conn;

                cmd.CommandText = "SELECT * from loginDetails WHERE loginID = @ID;";
                cmd.Parameters.AddWithValue("@ID", ID);
                if(cmd.ExecuteScalar() != null)
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
            private void Output(object output)
            {
                //Easier to call this than write the writeLine everytime
                //Would have been more useful if I used it
                Console.WriteLine(output);
            }
            public void Delete(String ID)
            {
                //Removes information stored under a given loginID
                if (debug) Console.WriteLine($"Delete record '{ID}' from DataBase");
                var cmd = new MySqlCommand();
                cmd.Connection= conn;
                cmd.CommandText = "DELETE phonenumber FROM phonenumber " +
                    "WHERE userID = (SELECT userID FROM logindetails WHERE loginID = @ID); " +

                    "DELETE emails, usersemail FROM usersemail INNER JOIN emails ON emails.emailID = usersemail.emailID " +
                    "WHERE usersemail.userID = (SELECT userID FROM loginDetails WHERE loginID = @ID);" +

                    "DELETE users, logindetails FROM logindetails INNER JOIN users ON logindetails.userID = users.userID " +
                    "WHERE loginID = @ID;" +
                cmd.Parameters.AddWithValue("@ID", ID);
                if(cmd.ExecuteNonQuery() != 0)
                {
                    Console.WriteLine($"User with ID {ID} has been deleted");
                }
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
                    "AND logindetails.userID = (SELECT userID FROM logindetails " +
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
                    }
                    Console.WriteLine(output);
                }
                if(output == "")
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
                    "AND users.userID = " +
                    "(SELECT userID FROM loginDetails WHERE loginID = @ID1) " +
                    "AND users.userID = usersemail.userID " +
                    "AND usersemail.emailID = emails.emailID " +
                    "AND phonenumber.userID = users.userID;";
                cmd.Parameters.AddWithValue("@ID", ID);
                cmd.Parameters.AddWithValue("@ID1", ID);

                cmd.ExecuteNonQuery();
                string output = string.Empty;
                if (field.ToLower() == "location" || field.ToLower() == "userlocation") field = "userLocation";

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader.GetName(i).Equals(field) && output != null)
                            {
                                output += $"{reader.GetName(i)}: {reader.GetString(i)}\n";
                            }
                        }
                    }
                }
                if (output != "")
                {
                    Console.WriteLine(output);
                    return output;
                }
                else
                {
                    //Shouldn't happen
                    Console.WriteLine($"Error: Cannot find {ID}");
                    return null;
                }
            }
            public void AddNewUser(string ID, string field, string value)
            {
                //Adds new user to DB
                //generates new userID
                Random rnd = new Random();
                string userID = string.Empty;
                do 
                {
                    userID = rnd.Next(100000,999999).ToString();
                    Console.WriteLine(userID);
                } while (CheckDBID(userID));
                MySqlCommand cmd = new MySqlCommand();
                var ins = new MySqlCommand();
                ins.Connection = conn;
                cmd.Connection = conn;

                cmd.CommandText = "INSERT INTO users(userID, userLocation, forenames, surname, title, position) VALUES(@userID, @location, ' ',' ',' ',' '); " +
                    "INSERT INTO emails(email) VALUES(' '); " +
                    "INSERT INTO usersemail(userID, emailID) VALUES( @userID, (SELECT LAST_INSERT_ID()) ); " +
                    "INSERT INTO phonenumber(userID, phone) VALUES(@userID, ' ');";
                cmd.Parameters.AddWithValue("@location", value);
                cmd.Parameters.AddWithValue("@userID", userID);
                //cmd.Parameters.AddWithValue("@userID1", userID);
                //cmd.Parameters.AddWithValue("@userID2", userID);

                ins.CommandText = "INSERT INTO loginDetails(userID, loginID) VALUES (@userID, @loginID);";
                ins.Parameters.AddWithValue("@userID", userID);
                ins.Parameters.AddWithValue("@loginID", ID);
                cmd.ExecuteNonQuery();
                ins.ExecuteNonQuery();
                conn.Close();

                Console.WriteLine("Added new user");
            }
            public void Update(string ID, string field, string update)
            {
                //Updates given field with given loginID and update info
                MySqlCommand cmd = new MySqlCommand();
                switch (field.ToLower())
                {
                    case "userlocation":
                    case "location":
                        cmd.CommandText = "UPDATE Users SET userLocation = @update WHERE userID = " +
                            "(SELECT userID FROM logindetails WHERE loginID = @ID);";
                        break;
                    case "forename":
                        cmd.CommandText = "UPDATE Users SET forenames = @update WHERE userID = " +
                        "(SELECT userID FROM logindetails WHERE loginID = @ID);";
                        break;
                    case "lastname":
                        cmd.CommandText = "UPDATE Users SET surname = @update WHERE userID = " +
                        "(SELECT userID FROM logindetails WHERE loginID = @ID);";
                        break;
                    case "title":
                        cmd.CommandText = "UPDATE Users SET title = @update WHERE userID = " +
                        "(SELECT userID FROM logindetails WHERE loginID = @ID);";
                        break;
                    case "position":
                        cmd.CommandText = "UPDATE Users SET position = @update WHERE userID = " +
                        "(SELECT userID FROM logindetails WHERE loginID = @ID);";
                        break;
                    case "phone":
                        cmd.CommandText = "UPDATE phonenumber, users SET phone = @update WHERE users.userID = " +
                            "(SELECT userID FROM loginDetails WHERE loginID = @ID) " +
                            "AND phonenumber.userID = users.userID;";
                        break;
                    case "email":
                        cmd.CommandText = "UPDATE emails SET email = @update WHERE emailID = " +
                            "(SELECT emailID FROM usersEmail WHERE userID = " +
                            "(SELECT userID FROM loginDetails WHERE loginID = @ID));";
                        break;
                    default:
                        Console.WriteLine($"Unkown field {field}");
                        return;

                }
                cmd.Parameters.AddWithValue("@update", update);
                cmd.Parameters.AddWithValue("@ID", ID);

                cmd.Connection = conn;
                if (cmd.ExecuteNonQuery() >=1)
                {
                    //cmd.ExecuteNonQuery();
                    Console.WriteLine($"Updated {field} to {update} for {ID}");
                    conn.Close();
                }
                else
                {
                    Console.WriteLine("Cannot update user");
                    ////Should only happen when the user doesn't have a listed phone/email so shouldn't happen 
                    //Console.WriteLine("Add to table");
                    //var ins = new MySqlCommand();
                    //ins.Connection = conn;
                    //if (field.ToLower() == "phone")
                    //{
                    //    //User has no listed phonenumber
                    //    ins.CommandText = "INSERT INTO phonenumber(phone, userID) VALUES(@update, (SELECT userID FROM logindetails WHERE loginID = @ID));";
                    //    ins.Parameters.AddWithValue("@update", update);
                    //    ins.Parameters.AddWithValue("@ID", ID);
                    //    ins.ExecuteNonQuery();
                    //    conn.Close();
                    //}
                    //else if (field.ToLower() == "email")
                    //{
                    //    Console.WriteLine("Add email");
                    //    //User has no listed email
                    //    ins.CommandText = "INSERT INTO emails(email) VALUES(@email);" +
                    //        "INSERT INTO usersemail(userID, emailID) VALUES( (SELECT userID FROM logindetails WHERE loginID = @ID),(SELECT LAST_INSERT_ID()) );";
                    //    ins.Parameters.AddWithValue("@email", update);
                    //    ins.Parameters.AddWithValue("@ID", ID);
                    //    //ins.Parameters.AddWithValue("@emailID", emailID);
                    //    if (ins.ExecuteNonQuery() != 0)
                    //    {
                    //        ins.Dispose();
                    //        conn.Close();

                    //        conn.Open();
                    //        ins = new MySqlCommand();
                    //        ins.Connection = conn;
                    //        ins.CommandText = "INSERT userID, emailID INTO usersemail VALUES((SELECT userID FROM loginDetails WHERE loginID = @ID),(SELECT emailID FROM emails WHERE );";
                    //        //ins.CommandText = "INSERT emailID, email INTO email VALUES(@emailID, @email);";
                    //        //ins.Parameters.AddWithValue("@emailID", emailID);
                    //        ins.Parameters.AddWithValue("@ID", ID);
                    //        // ins.ExecuteNonQuery();
                    //    }
                    //}
                    //else
                    //{
                    //    //Should never happen
                    //    Console.WriteLine("Unable to update database");
                    //}
                }
            }
        }
        static string HTTPUpdate(string line, StreamReader sr)
        {
            //Update request from webpage
            Console.WriteLine(line);
            String[] slices = line.Split(new char[] { '&' }, 2);
            String ID = slices[0].Substring(5);
            String value = slices[1].Substring(13);
            if (debug) Console.WriteLine($"Received an update request for '{ID}' to '{value}'");
            string conStr = string.Empty;
            MySqlConnection conn = new MySqlConnection("Server=localhost; user=root;" +
            "database=whois;port=3306;password=L3tM31n;");

            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            conn.Open();

            cmd.CommandText = "UPDATE whois.users SET userLocation = @location WHERE userID = " +
                "(SELECT userID FROM logindetails WHERE loginID = @ID);";
            cmd.Parameters.AddWithValue("@location", value);
            cmd.Parameters.AddWithValue("@ID", ID);
            cmd.ExecuteNonQuery();
            conn.Close();
            return $"Updated Users location with ID: {ID} to: {value}";
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
                    Console.WriteLine("Line: " + line);
                    // line = socketStream.Read(content_length);
                    line = "";
                    for (int i = 0; i < content_length; i++) line += (char)sr.Read();

                    String[] slices = line.Split(new char[] { '&' }, 2);
                    if (slices.Length < 2 //||
                                          //slices[0].Substring(7,5) != "name=" || 
                                          //slices[1].Substring(0,13) != "userLocation="
                        )
                    {
                        // This is an invalid request
                        sw.WriteLine("HTTP/1.1 400 Bad Request");
                        sw.WriteLine("Content-Type: text/plain");
                        sw.WriteLine();
                        sw.Flush();
                        Console.WriteLine($"Unrecognised command: '{line}'");
                        return;
                    }
                    //String ID = slices[0].Substring(5);    // The bit after name=
                    //String value = slices[1].Substring(9); // The bit after location=
                    string output = HTTPUpdate(line, sr);
                    //Console.Write(output);
                    sw.WriteLine("HTTP/1.1 200 OK");
                    sw.WriteLine("Content-Type: text/plain");
                    sw.WriteLine();
                    sw.WriteLine(output);
                    sw.Flush();
                }
                else if (line.StartsWith("GET") && line.EndsWith("HTTP/1.1"))
                {
                    // then we have a lookup
                    if (debug) Console.WriteLine("Received a lookup request");

                    String[] slices = line.Split(" ");  // Split into 3 pieces
                    String ID = slices[1].Substring(7);  // start at the 7th letter of the middle slice - skip `/?name=`

                    sw.WriteLine("HTTP/1.1 200 OK");
                    sw.WriteLine("Content-Type: text/plain");
                    sw.WriteLine();//Blank line IS IMPORTANT
                    string output = sc.Lookup(ID, "userLocation");
                    if (output != "")
                    {
                        sw.WriteLine($"Lookup performed on ID: {ID} <br>");
                        sw.WriteLine($"Result: {output}");
                    }
                    else
                    {
                        sw.WriteLine("User Not Found");
                    }
                    sw.Flush();

                    Console.WriteLine(ID);
                    Console.WriteLine(line);
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
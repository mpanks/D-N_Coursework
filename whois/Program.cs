﻿namespace whois
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
                    //switch (field) //TODO complete switch-case
                    //               //Add the rest of the fields
                    //{
                    //    case "userLocation":
                    //        break;
                    //    default:
                    //        Console.WriteLine($"Unknown field name: '{field}'");
                    //        return;


                    //}
                }
                if (debug) Console.Write($"Operation on ID '{ID}'");
                //TODO implement trying to add user to DB
                //if db !contain ID{add user}
                ServerCommands servCmd = new ServerCommands("localhost", "root", "whois", "3306", "P@55w0rd5");
                if (operation == null)
                {
                    servCmd.Dump(ID);
                }
                else if (operation == null || update == null) //TODO complete conditional
                                                              //&& DB !contain ID
                {
                    Console.WriteLine($"User {ID} is unknown");
                }
                else if (operation == "")
                {
                    servCmd.Delete(ID);
                    return;
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
            "database=whois;port=3306;password=P@55w0rd5";

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
            private void Output(object output)
            {
                Console.WriteLine(output);
            }
            public void Delete(String ID)
            {
                if (debug) Console.WriteLine($"Delete record '{ID}' from DataBase");
                //DataBase.Remove(ID);
                //TODO Implement DROP command to delete row - NOT THE DATABASE
            }
            public string Dump(string ID)
            {
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
                conn.Close();
                return output;
            }
            public string Lookup(string ID, string field)
            {
                MySqlCommand cmd = new MySqlCommand();
                cmd.CommandText = "SELECT * " +
                    " FROM users, logindetails, emails, phonenumber, usersemail " +
                    "WHERE logindetails.loginID = @ID " +
                    "AND users.userID = logindetails.userID " +
                    "AND users.userID = usersemail.userID " +
                    "AND usersemail.emailID = emails.emailID;";
                cmd.Parameters.AddWithValue("@ID", ID);

                cmd.Connection = conn;
                cmd.ExecuteNonQuery();
                string output = string.Empty;
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader.GetName(i).Equals(field))
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
                    //TODO Add unknown to database, create new void for simplicity
                    return null;
                }
            }
            private void AddNewUser(string ID, string field)
            {
                MySqlCommand cmd = new MySqlCommand();
                cmd.CommandText = "INSERT INTO ";
                cmd.Connection = conn;
            }
            public void Update(string ID, string field, string update)
            {
                MySqlCommand cmd = new MySqlCommand();
                switch (field)
                {
                    case "userLocation":
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
                    default:
                        Console.WriteLine($"Unkown field {field}");
                        break;

                }
                //cmd.CommandText = "UPDATE Users,emails,useremails,logindetails,phonenumber,\n" +
                   // "SET userLocation = @update \n" +
                    //"WHERE Users.UserID = \n" +
                    //"(SELECT UserID FROM logindetails \n" +
                    //"WHERE loginID = @ID);";
                //cmd.Parameters.AddWithValue("@field", field);
                cmd.Parameters.AddWithValue("@update", update);
                cmd.Parameters.AddWithValue("@ID", ID);

                cmd.Connection = conn;
                if(cmd.ExecuteScalar != null)
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"Updated {field} to {update} for {ID}");
                    conn.Close();
                }
                else
                {
                    //AddNewUser
                }
                //if (cmd.ExecuteNonQuery() < 1)
                //{
                //    //Add unknown user with the given field
                //    Console.WriteLine($"Added user {ID} with {field} = {update}");
                //    cmd.Connection.Close();
                //}
                //else
                //{
                //    Console.WriteLine($"ID: {ID} has been updated with {field} = {update}");
                //    cmd.Connection.Close ();
                //}
            }
        }
        static string HTTPUpdate(string line, StreamReader sr)
        {

            Console.WriteLine(line);
            String[] slices = line.Split(new char[] { '&' }, 2);
            String ID = slices[0].Substring(5);
            String value = slices[1].Substring(13);
            if (debug) Console.WriteLine($"Received an update request for '{ID}' to '{value}'");
            //TODO implement update request to update the DB
            string conStr = string.Empty;
            MySqlConnection conn = new MySqlConnection("Server=localhost; user=root;" +
            "database=whois;port=3306;password=P@55w0rd5;");

            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection=conn;
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
            //TODO implement HTML replies to website
            ServerCommands sc = new ServerCommands("localhost", "root", "whois", "3306", "P@55w0rd5");
            StreamWriter sw = new StreamWriter(socketStream);
            StreamReader sr = new StreamReader(socketStream);

            if (debug) Console.WriteLine("Waiting for input from client...");
            try
            {
                String line = sr.ReadLine();
                Console.WriteLine($"Received Network Command: '{line}'");
                //TODO implement HTTP add user
                //if db !contain id{add user}

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
                    Console.WriteLine("Line: "+line);
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
                        sw.WriteLine($"{output}");
                    }
                    else
                    {
                        sw.WriteLine("User Not Found");
                    }
                    sw.Flush();

                    Console.WriteLine(ID);
                    Console.WriteLine(line);
                    //if (DataBase.ContainsKey(ID))
                    //{
                    //    String result = DataBase[ID].Location;
                    //}
                    //else
                    //{
                    //    // Not found
                    //}
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
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
        static bool debug = true;
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
            Console.WriteLine($"\nCommand: {command}");
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
            Console.Write($"Operation on ID '{ID}'");
            ServerCommands servCmd = new ServerCommands("localhost", "root", "whois", "3306", "P@55w0rd5");
            if (operation == null)
            {
                servCmd.Dump(ID);
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
            public void Dump(string ID)
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
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    string output = "";
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
            }
            public void Lookup(string ID, string field)
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
                }
                else
                {
                    //TODO Add unknown to database, create new void for simplicity
                    
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
                cmd.CommandText = "UPDATE Users,emails,useremails,logindetails,phonenumber,\n" +
                    "SET userLocation = @update \n" +
                    "WHERE Users.UserID = \n" +
                    "(SELECT UserID FROM logindetails \n" +
                    "WHERE loginID = @ID);";
                cmd.Parameters.AddWithValue("@field", field);
                cmd.Parameters.AddWithValue("@update", update);
                cmd.Parameters.AddWithValue("@ID", ID);

                cmd.Connection = conn;

                cmd.ExecuteNonQuery();
                if (cmd.ExecuteNonQuery() < 1)
                {
                    //Add unknown user with the given field
                    Console.WriteLine($"Added user {ID} with {field} = {update}");
                    cmd.Connection.Close();
                }
                else
                {
                    Console.WriteLine($"ID: {ID} has been updated with {field} = {update}");
                    cmd.Connection.Close ();
                }
            }
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
            StreamWriter sw = new StreamWriter(socketStream);
            StreamReader sr = new StreamReader(socketStream);

            if (debug) Console.WriteLine("Waiting for input from client...");
            String line = sr.ReadLine();
            Console.WriteLine($"Received Network Command: '{line}'");

            if (line == "POST / HTTP/1.1")
            {
                // The we have an update
                if (debug) Console.WriteLine("Received an update request");
            }
            else if (line.StartsWith("GET /?name=") && line.EndsWith(" HTTP/1.1"))
            {
                // then we have a lookup
                if (debug) Console.WriteLine("Received a lookup request");
                String[] slices = line.Split(" ");  // Split into 3 pieces
                String ID = slices[1].Substring(7);  // start at the 7th letter of the middle slice - skip `/?name=`

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
    }
}
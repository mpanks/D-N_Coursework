﻿namespace whois
{
    using MySql.Data;
    using MySql.Data.MySqlClient;
    using Org.BouncyCastle.Asn1.Utilities;
    using System.Reflection.Metadata;

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Server");

            if (args.Length == 0)
            {

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
                    "WHERE loginID = @loginID)" +
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
                string output = "";
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
                if (output != null)
                {
                    Console.WriteLine(output);
                }
                else
                {
                    //Add unknown to database, create new void for simplicity
                }
            }
            private void AddNewUser(string ID, string field)
            {

            }
            public void Update(string ID, string field, string update)
            {
                string sqlCmd = $"UPDATE UserInfo SET {field} = '{update}' WHERE UserID='{ID}';";
                MySqlCommand cmd = new MySqlCommand(sqlCmd, conn);
                if (cmd.ExecuteNonQuery() < 1)
                {
                    //Add unknown user with the given field
                    Console.WriteLine($"Added user {ID} with {field} = {update}");
                }
                else
                {
                    Console.WriteLine($"ID: {ID} has been updated with {field} = {update}");
                }
            }
        }

        static void OpenConnection()
        {
            //nada
        }
    }

}
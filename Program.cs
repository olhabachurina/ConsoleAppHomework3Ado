// See https://aka.ms/new-console-template for more information
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
class Program
{
    public static string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=AccountBalance;Integrated Security=True;TrustServerCertificate=True;";

    static void Main()
    {
        int senderAccountID = 1;
        int receiverAccountID = 2;
        decimal amount = 500.00m;
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                PerformTransaction(connection, transaction, senderAccountID, receiverAccountID, amount);
                transaction.Commit();
                Console.WriteLine("Транзакция выполнена успешно.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
                if (transaction.Connection != null && transaction.Connection.State == ConnectionState.Open)
                {
                    transaction.Rollback();
                }
            }
            DisplayTransactionHistory(connection);
        }
    }
    static void PerformTransaction(SqlConnection connection, SqlTransaction transaction, int senderAccountID, int receiverAccountID, decimal amount)
    {
        decimal senderBalance = GetAccountBalance(connection, transaction, senderAccountID);

        if (senderBalance < amount || !AccountExists(connection, transaction, senderAccountID) || !AccountExists(connection, transaction, receiverAccountID))
        {
            InsertTransactionHistory(connection, transaction, senderAccountID, receiverAccountID, amount, "Failed");
            Console.WriteLine("Ошибка: недостаточно средств на счете отправителя или аккаунты не существуют. Транзакция отменена.");
            return;
        }
        UpdateAccountBalance(connection, transaction, senderAccountID, senderBalance - amount);
        UpdateAccountBalance(connection, transaction, receiverAccountID, GetAccountBalance(connection, transaction, receiverAccountID) + amount);
        InsertTransactionHistory(connection, transaction, senderAccountID, receiverAccountID, amount, "Success");
        Console.WriteLine("Транзакция выполнена успешно.");
    }

    static bool AccountExists(SqlConnection connection, SqlTransaction transaction, int accountID)
    {
        using (SqlCommand command = new SqlCommand($"SELECT COUNT(*) FROM Accounts WHERE AccountID = @AccountID", connection, transaction))
        {
            command.Parameters.AddWithValue("@AccountID", accountID);
            int count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }
    }

    static decimal GetAccountBalance(SqlConnection connection, SqlTransaction transaction, int accountID)
    {
        using (SqlCommand command = new SqlCommand("SELECT Balance FROM Accounts WHERE AccountID = @AccountID", connection, transaction))
        {
            command.Parameters.AddWithValue("@AccountID", accountID);
            return Convert.ToDecimal(command.ExecuteScalar());
        }
    }

    static void UpdateAccountBalance(SqlConnection connection, SqlTransaction transaction, int accountID, decimal newBalance)
    {
        using (SqlCommand command = new SqlCommand("UPDATE Accounts SET Balance = @NewBalance WHERE AccountID = @AccountID", connection, transaction))
        {
            command.Parameters.AddWithValue("@NewBalance", newBalance);
            command.Parameters.AddWithValue("@AccountID", accountID);
            command.ExecuteNonQuery();
        }
    }

    static void InsertTransactionHistory(SqlConnection connection, SqlTransaction transaction, int senderAccountID, int receiverAccountID, decimal amount, string status)
    {
        if (!AccountExists(connection, transaction, senderAccountID) || !AccountExists(connection, transaction, receiverAccountID))
        {
            Console.WriteLine("Ошибка: Аккаунт отправителя или получателя не найден.");
            transaction.Rollback();
            return;
        }

        using (SqlCommand command = new SqlCommand("INSERT INTO TransactionHistory (FromAccountID, ToAccountID, Amount, TransactionDate) VALUES (@SenderAccountID, @ReceiverAccountID, @Amount, GETDATE())", connection, transaction))
        {
            command.Parameters.AddWithValue("@SenderAccountID", senderAccountID);
            command.Parameters.AddWithValue("@ReceiverAccountID", receiverAccountID);
            command.Parameters.AddWithValue("@Amount", amount);

            command.ExecuteNonQuery();
        }
    }

    static void DisplayTransactionHistory(SqlConnection connection)
    {
        {
            using (SqlCommand cmd = new SqlCommand("SELECT * FROM TransactionHistory", connection))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"TransactionID: {reader["TransactionID"]}, FromAccountID: {reader["FromAccountID"]}, ToAccountID: {reader["ToAccountID"]}, Amount: {reader["Amount"]}, TransactionDate: {reader["TransactionDate"]}");
                    }
                }
            }
        }
    }
}



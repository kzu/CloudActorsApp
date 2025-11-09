using System.Runtime.Serialization;
using Devlooped.CloudActors;

namespace TestDomain;

// NOTE: there's NO references or dependencies to any Orleans concepts. Just
// pure CloudActors interfaces and attributes.

public partial record Deposit(decimal Amount) : IActorCommand;

public partial record Withdraw(decimal Amount) : IActorCommand;

public partial record Close(CloseReason Reason = CloseReason.Customer) : IActorCommand<decimal>;

public enum CloseReason
{
    Customer,
    Fraud,
    Other
}

public partial record GetBalance() : IActorQuery<decimal>;

[Actor]
public partial class Account
{
    public Account(string id) => Id = id;

    [IgnoreDataMember]
    public string Id { get; }

    public decimal Balance { get; private set; }

    public bool IsClosed { get; private set; }

    public CloseReason Reason { get; private set; }

    // Showcases that operation can also be just Execute overloads 
    //public void Execute(Deposit command)
    //{
    //    // validate command
    //    Raise(new Deposited(command.Amount));
    //}
    //public void Execute(Withdraw command)
    //{
    //    // validate command
    //    Raise(new Withdraw(command.Amount));
    //}

    // Showcases that operations can have a name that's not Execute
    public void Deposit(Deposit command)
    {
        if (IsClosed)
            throw new InvalidOperationException("Account is closed.");

        // validate command
        Balance += command.Amount;
    }

    // Showcases that operations don't have to be async
    public void Withdraw(Withdraw command)
    {
        if (IsClosed)
            throw new InvalidOperationException("Account is closed.");

        if (command.Amount > Balance)
            throw new InvalidOperationException("Insufficient funds.");

        Balance -= command.Amount;
    }

    // Showcases value-returning async operation with custom name.
    public decimal Close(Close command)
    {
        if (IsClosed)
            throw new InvalidOperationException("Account is already closed.");

        var final = Balance;

        Balance = 0;
        IsClosed = true;
        Reason = command.Reason;

        return final;
    }

    // Showcases a query that doesn't change state, which becomes a [ReadOnly] grain operation.
    public decimal Query(GetBalance _) => Balance;
}
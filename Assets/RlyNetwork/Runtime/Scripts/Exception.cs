#nullable enable

using System;

public class MissingWalletException : Exception
{
    public MissingWalletException()
        : base("Unable to perform action, no account on device")
    {
    }
}

public class InsufficientBalanceException : Exception
{
    public InsufficientBalanceException()
        : base("Unable to transfer, insufficient balance")
    {
    }
}

public class PriorDustingException : Exception
{
    public PriorDustingException()
        : base("Account already dusted, will not dust again")
    {
    }
}

public class RelayErrorException : Exception
{
    public RelayErrorException()
        : base("Unable to perform action, transaction relay error")
    {
    }
}

public class TransferMethodNotSupportedException : Exception
{
    public TransferMethodNotSupportedException()
        : base("This token does not have a transfer method supported by this sdk")
    {
    }
}

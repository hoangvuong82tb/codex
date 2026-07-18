namespace ASMILE.BO.Core.Enums;

public enum AccountingPeriodStatus
{
    Open = 0,
    Locked = 1,
    Closed = 2
}

public enum JournalStatus
{
    Draft = 0,
    Posted = 1,
    Reversed = 2,
    Cancelled = 3
}

public enum PostingBatchStatus
{
    Draft = 0,
    Previewed = 1,
    Posted = 2,
    Failed = 3,
    Reversed = 4
}

public enum DebitCreditSide
{
    Debit = 0,
    Credit = 1
}

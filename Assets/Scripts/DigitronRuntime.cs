using System.Globalization;

public enum DigitronKeyId
{
    Power,
    K,
    F,
    ClearEntry,
    ClearAll,
    Seven,
    Eight,
    Nine,
    MinusOrEquals,
    Four,
    Five,
    Six,
    Divide,
    One,
    Two,
    Three,
    Multiply,
    Zero,
    Decimal,
    Add,
}

public sealed class DigitronRuntime
{
    private enum PendingOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
    }

    private const string KZeroDisplay = "0";
    private const string KErrorDisplay = "Error";
    private const int KMaxDisplayLength = 10;

    private decimal m_Accumulator;
    private bool m_HasAccumulator;
    private PendingOperation? m_PendingOperation;
    private bool m_IsEnteringNewNumber;
    private string m_CurrentEntry = KZeroDisplay;

    public bool IsPoweredOn { get; private set; }
    public bool IsError { get; private set; }
    public string DisplayText => IsPoweredOn ? (IsError ? KErrorDisplay : m_CurrentEntry) : string.Empty;

    public void ResetPoweredOff()
    {
        IsPoweredOn = false;
        ResetInternalState();
    }

    public void PressKey(DigitronKeyId keyId)
    {
        if (keyId == DigitronKeyId.Power)
        {
            TogglePower();
            return;
        }

        if (!IsPoweredOn)
        {
            return;
        }

        if (IsError && keyId != DigitronKeyId.ClearEntry && keyId != DigitronKeyId.ClearAll)
        {
            return;
        }

        switch (keyId)
        {
            case DigitronKeyId.K:
            case DigitronKeyId.F:
                return;
            case DigitronKeyId.ClearEntry:
                ClearEntry();
                return;
            case DigitronKeyId.ClearAll:
                ResetPoweredOn();
                return;
            case DigitronKeyId.Seven:
                AppendDigit('7');
                return;
            case DigitronKeyId.Eight:
                AppendDigit('8');
                return;
            case DigitronKeyId.Nine:
                AppendDigit('9');
                return;
            case DigitronKeyId.Four:
                AppendDigit('4');
                return;
            case DigitronKeyId.Five:
                AppendDigit('5');
                return;
            case DigitronKeyId.Six:
                AppendDigit('6');
                return;
            case DigitronKeyId.One:
                AppendDigit('1');
                return;
            case DigitronKeyId.Two:
                AppendDigit('2');
                return;
            case DigitronKeyId.Three:
                AppendDigit('3');
                return;
            case DigitronKeyId.Zero:
                AppendDigit('0');
                return;
            case DigitronKeyId.Decimal:
                AppendDecimalSeparator();
                return;
            case DigitronKeyId.Add:
                QueueOperation(PendingOperation.Add);
                return;
            case DigitronKeyId.Divide:
                QueueOperation(PendingOperation.Divide);
                return;
            case DigitronKeyId.Multiply:
                QueueOperation(PendingOperation.Multiply);
                return;
            case DigitronKeyId.MinusOrEquals:
                HandleMinusOrEquals();
                return;
        }
    }

    private void TogglePower()
    {
        if (IsPoweredOn)
        {
            ResetPoweredOff();
            return;
        }

        IsPoweredOn = true;
        ResetPoweredOn();
    }

    private void ResetPoweredOn()
    {
        ResetInternalState();
        IsPoweredOn = true;
    }

    private void ResetInternalState()
    {
        IsError = false;
        m_Accumulator = 0m;
        m_HasAccumulator = false;
        m_PendingOperation = null;
        m_IsEnteringNewNumber = true;
        m_CurrentEntry = KZeroDisplay;
    }

    private void ClearEntry()
    {
        IsError = false;
        m_CurrentEntry = KZeroDisplay;
        m_IsEnteringNewNumber = true;
    }

    private void AppendDigit(char digit)
    {
        if (m_IsEnteringNewNumber)
        {
            m_CurrentEntry = digit.ToString();
            m_IsEnteringNewNumber = false;
        }
        else if (m_CurrentEntry == KZeroDisplay)
        {
            m_CurrentEntry = digit.ToString();
        }
        else if (m_CurrentEntry.Length < KMaxDisplayLength)
        {
            m_CurrentEntry += digit;
        }
    }

    private void AppendDecimalSeparator()
    {
        if (m_IsEnteringNewNumber)
        {
            m_CurrentEntry = "0,";
            m_IsEnteringNewNumber = false;
            return;
        }

        if (!m_CurrentEntry.Contains(",") && m_CurrentEntry.Length < KMaxDisplayLength)
        {
            m_CurrentEntry += ",";
        }
    }

    private void HandleMinusOrEquals()
    {
        if (m_PendingOperation.HasValue && !m_IsEnteringNewNumber)
        {
            ExecuteEquals();
            return;
        }

        QueueOperation(PendingOperation.Subtract);
    }

    private void QueueOperation(PendingOperation operation)
    {
        if (!TryParseCurrentEntry(out var currentValue))
        {
            SetErrorState();
            return;
        }

        if (!m_HasAccumulator)
        {
            m_Accumulator = currentValue;
            m_HasAccumulator = true;
        }
        else if (!m_IsEnteringNewNumber && m_PendingOperation.HasValue)
        {
            if (!TryEvaluate(m_Accumulator, currentValue, m_PendingOperation.Value, out var result))
            {
                SetErrorState();
                return;
            }

            m_Accumulator = result;
            m_CurrentEntry = FormatValue(result);
        }
        else
        {
            m_CurrentEntry = FormatValue(m_Accumulator);
        }

        m_PendingOperation = operation;
        m_IsEnteringNewNumber = true;
        m_CurrentEntry = FormatValue(m_Accumulator);
    }

    private void ExecuteEquals()
    {
        if (!m_PendingOperation.HasValue || !TryParseCurrentEntry(out var currentValue))
        {
            return;
        }

        if (!m_HasAccumulator)
        {
            m_Accumulator = currentValue;
            m_HasAccumulator = true;
        }
        else if (!TryEvaluate(m_Accumulator, currentValue, m_PendingOperation.Value, out var result))
        {
            SetErrorState();
            return;
        }
        else
        {
            m_Accumulator = result;
        }

        m_CurrentEntry = FormatValue(m_Accumulator);
        m_PendingOperation = null;
        m_IsEnteringNewNumber = true;
    }

    private bool TryEvaluate(decimal left, decimal right, PendingOperation operation, out decimal result)
    {
        switch (operation)
        {
            case PendingOperation.Add:
                result = left + right;
                return true;
            case PendingOperation.Subtract:
                result = left - right;
                return true;
            case PendingOperation.Multiply:
                result = left * right;
                return true;
            case PendingOperation.Divide:
                if (right == 0m)
                {
                    result = 0m;
                    return false;
                }

                result = left / right;
                return true;
            default:
                result = left;
                return true;
        }
    }

    private bool TryParseCurrentEntry(out decimal value)
    {
        var normalized = m_CurrentEntry.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private string FormatValue(decimal value)
    {
        var formatted = value.ToString("0.########", CultureInfo.InvariantCulture).Replace('.', ',');
        if (formatted.Length <= KMaxDisplayLength)
        {
            return formatted;
        }

        formatted = value.ToString("0.###E+0", CultureInfo.InvariantCulture).Replace('.', ',');
        return formatted.Length <= KMaxDisplayLength ? formatted : KErrorDisplay;
    }

    private void SetErrorState()
    {
        IsError = true;
        m_CurrentEntry = KErrorDisplay;
        m_IsEnteringNewNumber = true;
        m_PendingOperation = null;
        m_HasAccumulator = false;
        m_Accumulator = 0m;
    }
}

using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.QAReports.Forms.Controls;

/// <summary>
/// Edits one broken-data statistic and its non-sensitive explanation.
/// </summary>
public sealed class QaBrokenDataStatisticRowControl : UserControl
{
    private readonly Label fieldNameLabel;
    private readonly NumericUpDown brokenCountNumericUpDown;
    private readonly NumericUpDown nonblankCountNumericUpDown;
    private readonly CheckBox automaticNonblankCountCheckBox;
    private readonly TextBox percentageTextBox;
    private readonly TextBox explanationTextBox;

    private QaBrokenDataStatistic? boundStatistic;
    private bool hasPendingExplanationEdit;
    private bool isRefreshing;

    public QaBrokenDataStatisticRowControl()
    {
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BorderStyle = BorderStyle.FixedSingle;
        MinimumSize = new Size(520, 0);
        Margin = new Padding(0, 0, 0, 8);

        fieldNameLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(3, 0, 3, 6),
            Text = "Statistic field"
        };

        brokenCountNumericUpDown = CreateCountInput("Broken value count");
        nonblankCountNumericUpDown =
            CreateDenominatorInput("Derived applicable nonblank values");
        automaticNonblankCountCheckBox = CreateAutomaticCheckBox(
            "Calculate applicable nonblank values automatically");
        percentageTextBox = CreateReadOnlyValue("Broken-data percentage");
        explanationTextBox = new TextBox
        {
            AcceptsReturn = true,
            AccessibleDescription =
                "Enter a non-sensitive summary only; do not enter reservation-level data.",
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, 60),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true
        };

        FlowLayoutPanel metricsPanel = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            WrapContents = true
        };
        metricsPanel.Controls.Add(CreateMetricPanel(
            "Broken value count",
            brokenCountNumericUpDown));
        metricsPanel.Controls.Add(CreateMetricPanel(
            "Derived nonblank values",
            CreateDenominatorEditor(
                nonblankCountNumericUpDown,
                automaticNonblankCountCheckBox)));
        metricsPanel.Controls.Add(CreateMetricPanel(
            "Broken-data percentage",
            percentageTextBox));

        Label explanationLabel = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 4, 3, 4),
            Text = "Explanation (optional)"
        };

        TableLayoutPanel mainLayoutPanel = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Padding = new Padding(10, 8, 10, 8),
            RowCount = 4
        };
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int index = 0; index < mainLayoutPanel.RowCount; index++)
        {
            mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        mainLayoutPanel.Controls.Add(fieldNameLabel, 0, 0);
        mainLayoutPanel.Controls.Add(metricsPanel, 0, 1);
        mainLayoutPanel.Controls.Add(explanationLabel, 0, 2);
        mainLayoutPanel.Controls.Add(explanationTextBox, 0, 3);
        Controls.Add(mainLayoutPanel);

        brokenCountNumericUpDown.ValueChanged += (_, _) => ApplyUserValues();
        nonblankCountNumericUpDown.ValueChanged += (_, _) =>
            ApplyManualDenominatorValue();
        automaticNonblankCountCheckBox.CheckedChanged += (_, _) =>
            ApplyUserValues();
        explanationTextBox.TextChanged += (_, _) =>
            MarkExplanationEditPending();
        explanationTextBox.Validated += (_, _) =>
            CommitPendingTextEdits();
    }

    public string FieldId => boundStatistic?.FieldId ?? string.Empty;

    public event EventHandler? StatisticChanged;

    public void Bind(QaBrokenDataStatistic statistic)
    {
        ArgumentNullException.ThrowIfNull(statistic);

        if (string.IsNullOrWhiteSpace(statistic.FieldId))
        {
            throw new ArgumentException(
                "A broken-data statistic requires a nonblank stable field ID.",
                nameof(statistic));
        }

        bool bindingChanged = !ReferenceEquals(boundStatistic, statistic);

        if (bindingChanged && boundStatistic is not null)
        {
            bool sameFieldId = boundStatistic.FieldId.Equals(
                statistic.FieldId,
                StringComparison.Ordinal);
            _ = CommitPendingTextEditsCore(raiseStatisticChanged: false);

            if (sameFieldId)
            {
                statistic.Explanation = boundStatistic.Explanation;
            }
        }

        boundStatistic = statistic;
        if (bindingChanged)
        {
            hasPendingExplanationEdit = false;
        }

        RefreshFromStatistic();
    }

    public void RefreshFromStatistic()
    {
        QaBrokenDataStatistic statistic = GetBoundStatistic();
        bool wasRefreshing = isRefreshing;
        isRefreshing = true;

        try
        {
            fieldNameLabel.Text = statistic.DisplayName;
            AccessibleName = $"Broken data for {statistic.DisplayName}";
            brokenCountNumericUpDown.AccessibleName =
                $"Broken value count for {statistic.DisplayName}";
            nonblankCountNumericUpDown.AccessibleName =
                $"Derived applicable nonblank values for {statistic.DisplayName}";
            automaticNonblankCountCheckBox.AccessibleName =
                $"Calculate applicable nonblank values automatically for {statistic.DisplayName}";
            percentageTextBox.AccessibleName =
                $"Calculated broken-data percentage for {statistic.DisplayName}";
            explanationTextBox.AccessibleName =
                $"Broken-data explanation for {statistic.DisplayName}";

            SetCountValue(brokenCountNumericUpDown, statistic.BrokenValueCount);
            SetDenominatorValue(
                nonblankCountNumericUpDown,
                statistic.TotalApplicableNonblankValues);
            automaticNonblankCountCheckBox.Checked =
                statistic.UseAutomaticTotalApplicableNonblankValues;
            percentageTextBox.Text = FormatPercentage(
                statistic.BrokenDataPercentage);
            if (!hasPendingExplanationEdit)
            {
                SetOptionalText(explanationTextBox, statistic.Explanation);
            }
        }
        finally
        {
            isRefreshing = wasRefreshing;
        }
    }

    public void CommitCurrentValues()
    {
        QaBrokenDataStatistic statistic = GetBoundStatistic();
        statistic.BrokenValueCount =
            decimal.ToInt32(brokenCountNumericUpDown.Value);
        statistic.TotalApplicableNonblankValues =
            decimal.ToInt32(nonblankCountNumericUpDown.Value);
        statistic.UseAutomaticTotalApplicableNonblankValues =
            automaticNonblankCountCheckBox.Checked;
        statistic.BrokenDataPercentage =
            QaStatisticsCalculationService.CalculatePercentage(
                statistic.BrokenValueCount,
                statistic.TotalApplicableNonblankValues);
        _ = CommitPendingTextEditsCore(raiseStatisticChanged: false);
        percentageTextBox.Text = FormatPercentage(
            statistic.BrokenDataPercentage);
    }

    /// <summary>
    /// Commits the final normalized explanation and raises at most one logical
    /// statistic change event.
    /// </summary>
    public bool CommitPendingTextEdits()
    {
        return CommitPendingTextEditsCore(raiseStatisticChanged: true);
    }

    internal bool CommitPendingTextEditsWithoutNotification()
    {
        return CommitPendingTextEditsCore(raiseStatisticChanged: false);
    }

    public void DiscardPendingTextEdits()
    {
        hasPendingExplanationEdit = false;
    }

    private void ApplyUserValues()
    {
        if (isRefreshing || boundStatistic is null)
        {
            return;
        }

        CommitCurrentValues();
        StatisticChanged?.Invoke(this, EventArgs.Empty);
    }

    private void MarkExplanationEditPending()
    {
        if (!isRefreshing && boundStatistic is not null)
        {
            hasPendingExplanationEdit = true;
        }
    }

    private bool CommitPendingTextEditsCore(bool raiseStatisticChanged)
    {
        if (isRefreshing
            || !hasPendingExplanationEdit
            || boundStatistic is null)
        {
            return false;
        }

        string? explanation = TrimToNull(explanationTextBox.Text);
        bool changed = !string.Equals(
            boundStatistic.Explanation,
            explanation,
            StringComparison.Ordinal);

        if (changed)
        {
            boundStatistic.Explanation = explanation;
        }

        hasPendingExplanationEdit = false;

        if (changed && raiseStatisticChanged)
        {
            StatisticChanged?.Invoke(this, EventArgs.Empty);
        }

        return changed;
    }

    private void ApplyManualDenominatorValue()
    {
        if (isRefreshing || boundStatistic is null)
        {
            return;
        }

        isRefreshing = true;

        try
        {
            automaticNonblankCountCheckBox.Checked = false;
        }
        finally
        {
            isRefreshing = false;
        }

        ApplyUserValues();
    }

    private QaBrokenDataStatistic GetBoundStatistic()
    {
        return boundStatistic
            ?? throw new InvalidOperationException(
                "Bind must be called before using a broken-data statistic row.");
    }

    private static NumericUpDown CreateCountInput(string accessibleName)
    {
        return new NumericUpDown
        {
            AccessibleName = accessibleName,
            Maximum = int.MaxValue,
            Minimum = 0,
            Size = new Size(132, 23),
            ThousandsSeparator = true
        };
    }

    private static TextBox CreateReadOnlyValue(string accessibleName)
    {
        return new TextBox
        {
            AccessibleName = accessibleName,
            ReadOnly = true,
            Size = new Size(100, 23),
            TabStop = false,
            Text = "0.00%"
        };
    }

    private static NumericUpDown CreateDenominatorInput(string accessibleName)
    {
        return new NumericUpDown
        {
            AccessibleName = accessibleName,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
            Size = new Size(132, 23),
            ThousandsSeparator = true
        };
    }

    private static CheckBox CreateAutomaticCheckBox(string accessibleName)
    {
        return new CheckBox
        {
            AccessibleDescription =
                "Clear Auto to keep a manual denominator. Select Auto to calculate from the matching Blank statistic.",
            AccessibleName = accessibleName,
            AutoSize = true,
            Checked = true,
            Margin = new Padding(7, 3, 0, 0),
            Name = "automaticNonblankCountCheckBox",
            Text = "Auto"
        };
    }

    private static Control CreateDenominatorEditor(
        NumericUpDown valueControl,
        CheckBox automaticCheckBox)
    {
        FlowLayoutPanel panel = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            WrapContents = false
        };
        valueControl.Margin = Padding.Empty;
        panel.Controls.Add(valueControl);
        panel.Controls.Add(automaticCheckBox);
        return panel;
    }

    private static Control CreateMetricPanel(string labelText, Control valueControl)
    {
        Label label = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 3),
            Text = labelText
        };

        TableLayoutPanel panel = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Margin = new Padding(3, 0, 18, 6),
            RowCount = 2
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(label, 0, 0);
        panel.Controls.Add(valueControl, 0, 1);
        return panel;
    }

    private static void SetCountValue(NumericUpDown control, int value)
    {
        control.Value = Math.Clamp(value, 0, int.MaxValue);
    }

    private static void SetDenominatorValue(NumericUpDown control, int value)
    {
        control.Value = Math.Clamp(value, control.Minimum, control.Maximum);
    }

    private static void SetOptionalText(TextBox textBox, string? value)
    {
        if (!string.Equals(
                TrimToNull(textBox.Text),
                TrimToNull(value),
                StringComparison.Ordinal))
        {
            textBox.Text = value ?? string.Empty;
        }
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string FormatPercentage(decimal percentage)
    {
        return $"{percentage:0.00}%";
    }
}

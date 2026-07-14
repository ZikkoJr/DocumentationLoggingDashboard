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
    private readonly TextBox nonblankCountTextBox;
    private readonly TextBox percentageTextBox;
    private readonly TextBox explanationTextBox;

    private QaBrokenDataStatistic? boundStatistic;
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
        nonblankCountTextBox =
            CreateReadOnlyValue("Derived applicable nonblank values");
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
            nonblankCountTextBox));
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
        explanationTextBox.TextChanged += (_, _) => ApplyUserValues();
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

        boundStatistic = statistic;
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
            nonblankCountTextBox.AccessibleName =
                $"Derived applicable nonblank values for {statistic.DisplayName}";
            percentageTextBox.AccessibleName =
                $"Calculated broken-data percentage for {statistic.DisplayName}";
            explanationTextBox.AccessibleName =
                $"Broken-data explanation for {statistic.DisplayName}";

            SetCountValue(brokenCountNumericUpDown, statistic.BrokenValueCount);
            nonblankCountTextBox.Text =
                statistic.TotalApplicableNonblankValues.ToString("N0");
            percentageTextBox.Text = FormatPercentage(
                statistic.BrokenDataPercentage);
            SetOptionalText(explanationTextBox, statistic.Explanation);
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
        statistic.BrokenDataPercentage =
            QaStatisticsCalculationService.CalculatePercentage(
                statistic.BrokenValueCount,
                statistic.TotalApplicableNonblankValues);
        statistic.Explanation = TrimToNull(explanationTextBox.Text);
        percentageTextBox.Text = FormatPercentage(
            statistic.BrokenDataPercentage);
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

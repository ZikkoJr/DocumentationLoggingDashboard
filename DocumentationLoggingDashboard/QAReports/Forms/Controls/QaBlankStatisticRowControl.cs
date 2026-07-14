using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Services;

namespace DocumentationLoggingDashboard.QAReports.Forms.Controls;

/// <summary>
/// Edits one blank-value statistic while keeping its calculated percentage read-only.
/// </summary>
public sealed class QaBlankStatisticRowControl : UserControl
{
    private readonly Label fieldNameLabel;
    private readonly NumericUpDown blankCountNumericUpDown;
    private readonly NumericUpDown totalRowsNumericUpDown;
    private readonly TextBox percentageTextBox;

    private QaBlankValueStatistic? boundStatistic;
    private bool isRefreshing;

    public QaBlankStatisticRowControl()
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

        blankCountNumericUpDown = CreateCountInput("Blank count");
        totalRowsNumericUpDown = CreateCountInput("Total applicable rows");
        percentageTextBox = CreateReadOnlyValue("Blank percentage");

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
            "Blank count",
            blankCountNumericUpDown));
        metricsPanel.Controls.Add(CreateMetricPanel(
            "Total applicable rows",
            totalRowsNumericUpDown));
        metricsPanel.Controls.Add(CreateMetricPanel(
            "Blank percentage",
            percentageTextBox));

        TableLayoutPanel mainLayoutPanel = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Padding = new Padding(10, 8, 10, 8),
            RowCount = 2
        };
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayoutPanel.Controls.Add(fieldNameLabel, 0, 0);
        mainLayoutPanel.Controls.Add(metricsPanel, 0, 1);

        Controls.Add(mainLayoutPanel);

        blankCountNumericUpDown.ValueChanged += (_, _) => ApplyUserValues();
        totalRowsNumericUpDown.ValueChanged += (_, _) => ApplyUserValues();
    }

    public string FieldId => boundStatistic?.FieldId ?? string.Empty;

    public event EventHandler? StatisticChanged;

    public void Bind(QaBlankValueStatistic statistic)
    {
        ArgumentNullException.ThrowIfNull(statistic);

        if (string.IsNullOrWhiteSpace(statistic.FieldId))
        {
            throw new ArgumentException(
                "A blank-value statistic requires a nonblank stable field ID.",
                nameof(statistic));
        }

        boundStatistic = statistic;
        RefreshFromStatistic();
    }

    public void RefreshFromStatistic()
    {
        QaBlankValueStatistic statistic = GetBoundStatistic();
        bool wasRefreshing = isRefreshing;
        isRefreshing = true;

        try
        {
            fieldNameLabel.Text = statistic.DisplayName;
            AccessibleName = $"Blank values for {statistic.DisplayName}";
            blankCountNumericUpDown.AccessibleName =
                $"Blank count for {statistic.DisplayName}";
            totalRowsNumericUpDown.AccessibleName =
                $"Total applicable rows for {statistic.DisplayName}";
            percentageTextBox.AccessibleName =
                $"Calculated blank percentage for {statistic.DisplayName}";

            SetCountValue(blankCountNumericUpDown, statistic.BlankCount);
            SetCountValue(totalRowsNumericUpDown, statistic.TotalApplicableRows);
            percentageTextBox.Text = FormatPercentage(statistic.BlankPercentage);
        }
        finally
        {
            isRefreshing = wasRefreshing;
        }
    }

    public void CommitCurrentValues()
    {
        QaBlankValueStatistic statistic = GetBoundStatistic();
        statistic.BlankCount = decimal.ToInt32(blankCountNumericUpDown.Value);
        statistic.TotalApplicableRows = decimal.ToInt32(totalRowsNumericUpDown.Value);
        statistic.BlankPercentage =
            QaStatisticsCalculationService.CalculatePercentage(
                statistic.BlankCount,
                statistic.TotalApplicableRows);
        percentageTextBox.Text = FormatPercentage(statistic.BlankPercentage);
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

    private QaBlankValueStatistic GetBoundStatistic()
    {
        return boundStatistic
            ?? throw new InvalidOperationException(
                "Bind must be called before using a blank-value statistic row.");
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

    private static string FormatPercentage(decimal percentage)
    {
        return $"{percentage:0.00}%";
    }
}

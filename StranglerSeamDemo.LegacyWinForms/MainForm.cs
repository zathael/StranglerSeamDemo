using System.ComponentModel;
using StranglerSeamDemo.LegacyWinForms.Api;

namespace StranglerSeamDemo.LegacyWinForms;

public sealed class MainForm : Form
{
    // Controls
    private readonly TextBox _txtSearch = new() { PlaceholderText = "Search patient / procedure / status..." };
    private readonly Button _btnSearch = new() { Text = "Search" };
    private readonly ICasesGateway _gateway;
    private readonly bool _useApiSeam;


    private readonly DataGridView _grid = new()
    {
        ReadOnly = true,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoGenerateColumns = false,
        Dock = DockStyle.Fill
    };

    private readonly ComboBox _cmbStatus = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _btnUpdate = new() { Text = "Update Status" };
    private readonly Label _lblTotal = new() { AutoSize = true, Text = "Total: 0" };

    // State
    private readonly ApiClient _api = new("http://localhost:5050/");
    private int _page = 1;
    private const int PageSize = 10;

    // Bindable data source for grid
    private readonly BindingList<CaseDto> _rows = new();

    public MainForm(ICasesGateway gateway, bool useApiSeam)
    {
        _gateway = gateway;
        _useApiSeam = useApiSeam;

        Text = useApiSeam
            ? "StranglerSeamDemo Spike - WinForms (API Seam Enabled)"
            : "StranglerSeamDemo Spike - WinForms (Legacy DB Path Enabled)";

        Text = "StranglerSeamDemo Spike - Legacy WinForms (Service Seam Demo)";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 980;
        Height = 600;

        BuildLayout();
        ConfigureGrid();
        WireEvents();

        Shown += async (_, __) => await RefreshCasesAsync();
    }

    private void BuildLayout()
    {
        // Top bar: search
        var topBar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(10),
        };
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));

        _txtSearch.Dock = DockStyle.Fill;
        _btnSearch.Dock = DockStyle.Fill;

        // Optional: simple paging buttons for realism (still minimal)
        var pagingPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true
        };
        var btnPrev = new Button { Text = "Prev", Width = 60 };
        var btnNext = new Button { Text = "Next", Width = 60 };
        btnPrev.Click += async (_, __) => { if (_page > 1) { _page--; await RefreshCasesAsync(); } };
        btnNext.Click += async (_, __) => { _page++; await RefreshCasesAsync(); };
        pagingPanel.Controls.Add(btnPrev);
        pagingPanel.Controls.Add(btnNext);

        topBar.Controls.Add(_txtSearch, 0, 0);
        topBar.Controls.Add(_btnSearch, 1, 0);
        topBar.Controls.Add(pagingPanel, 2, 0);

        // Bottom bar: status update
        var bottomBar = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 54,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(10),
        };
        bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _cmbStatus.Dock = DockStyle.Fill;
        _btnUpdate.Dock = DockStyle.Fill;
        _lblTotal.Dock = DockStyle.Fill;
        _lblTotal.TextAlign = ContentAlignment.MiddleLeft;

        _cmbStatus.DataSource = new[] { "New", "InProgress", "OnHold", "Done", "Cancelled" };

        bottomBar.Controls.Add(new Label
        {
            Text = "Set status:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        bottomBar.Controls.Add(_cmbStatus, 1, 0);
        bottomBar.Controls.Add(_btnUpdate, 2, 0);
        bottomBar.Controls.Add(_lblTotal, 3, 0);

        Controls.Add(_grid);
        Controls.Add(topBar);
        Controls.Add(bottomBar);
    }

    private void ConfigureGrid()
    {
        // Explicit columns look more “senior” than AutoGenerateColumns
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Id",
            DataPropertyName = nameof(CaseDto.Id),
            Width = 60
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Patient",
            DataPropertyName = nameof(CaseDto.PatientName),
            Width = 220
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Procedure",
            DataPropertyName = nameof(CaseDto.Procedure),
            Width = 220
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Status",
            DataPropertyName = nameof(CaseDto.Status),
            Width = 120
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Last Updated (UTC)",
            DataPropertyName = nameof(CaseDto.LastUpdatedUtc),
            Width = 200,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "u" }
        });

        _grid.DataSource = _rows;
    }

    private void WireEvents()
    {
        _btnSearch.Click += async (_, __) =>
        {
            _page = 1;
            await RefreshCasesAsync();
        };

        _txtSearch.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _page = 1;
                await RefreshCasesAsync();
            }
        };

        _btnUpdate.Click += async (_, __) => await UpdateSelectedStatusAsync();

        // Double-click row to sync status dropdown to selection (nice polish)
        _grid.SelectionChanged += (_, __) =>
        {
            if (_grid.CurrentRow?.DataBoundItem is CaseDto selected)
            {
                var statuses = (string[])_cmbStatus.DataSource!;
                var match = statuses.FirstOrDefault(s => s.Equals(selected.Status, StringComparison.OrdinalIgnoreCase));
                if (match is not null) _cmbStatus.SelectedItem = match;
            }
        };
    }

    private async Task RefreshCasesAsync()
    {
        try
        {
            ToggleUi(false);

            var search = _txtSearch.Text?.Trim();
            var result = await _gateway.GetCasesAsync(search, _page, PageSize);

            _rows.RaiseListChangedEvents = false;
            _rows.Clear();
            foreach (var item in result.Items)
                _rows.Add(item);
            _rows.RaiseListChangedEvents = true;
            _rows.ResetBindings();

            _lblTotal.Text = $"Total: {result.Total}   |   Page: {result.Page}   |   PageSize: {result.PageSize}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not load cases.\n\n{ex.Message}\n\nIs the API running on http://localhost:5050 ?",
                "API error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            // If paging goes past end, pull back one page
            if (_page > 1) _page--;
        }
        finally
        {
            ToggleUi(true);
        }
    }

    private async Task UpdateSelectedStatusAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not CaseDto selected)
        {
            MessageBox.Show("Select a case first.", "No selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var status = _cmbStatus.SelectedItem?.ToString() ?? "New";

        try
        {
            ToggleUi(false);

            await _api.UpdateStatusAsync(selected.Id, status);

            // Refresh to show updated status + LastUpdatedUtc
            await RefreshCasesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not update status.\n\n{ex.Message}", "API error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleUi(true);
        }
    }

    private void ToggleUi(bool enabled)
    {
        _txtSearch.Enabled = enabled;
        _btnSearch.Enabled = enabled;
        _grid.Enabled = enabled;
        _cmbStatus.Enabled = enabled;
        _btnUpdate.Enabled = enabled;
    }
}

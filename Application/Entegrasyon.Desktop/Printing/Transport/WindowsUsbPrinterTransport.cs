using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Entegrasyon.PrintAgent.Contracts;

namespace Entegrasyon.Desktop.Printing.Transport;

[SupportedOSPlatform("windows")]
public class WindowsUsbPrinterTransport(ILogger<WindowsUsbPrinterTransport> logger) : ILocalPrinterTransport
{
    public async Task<PrintResult> SendRawAsync(string printerName, byte[] data, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
                return new PrintResult(false, $"Yazıcı açılamadı: {printerName}");

            try
            {
                var docInfo = new DOCINFOA { pDocName = "PrintAgent Job", pDataType = "RAW" };
                if (StartDocPrinter(hPrinter, 1, ref docInfo) == 0)
                    return new PrintResult(false, "StartDocPrinter başarısız");

                try
                {
                    if (!StartPagePrinter(hPrinter))
                        return new PrintResult(false, "StartPagePrinter başarısız");

                    var pBytes = Marshal.AllocCoTaskMem(data.Length);
                    try
                    {
                        Marshal.Copy(data, 0, pBytes, data.Length);
                        if (!WritePrinter(hPrinter, pBytes, data.Length, out var written))
                            return new PrintResult(false, "WritePrinter başarısız");

                        logger.LogInformation("USB yazıcıya gönderildi: {Printer}, {Written} byte", printerName, written);
                        return new PrintResult(true, "Yazdırma komutu gönderildi");
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(pBytes);
                        EndPagePrinter(hPrinter);
                    }
                }
                finally
                {
                    EndDocPrinter(hPrinter);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "USB yazdırma hatası: {Printer}", printerName);
                return new PrintResult(false, $"Yazdırma hatası: {ex.Message}");
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }, ct);
    }

    public Task<PrinterStatus> GetStatusAsync(string printerName, CancellationToken ct)
    {
        if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
            return Task.FromResult(new PrinterStatus(printerName, false, "Yazıcı bulunamadı"));

        ClosePrinter(hPrinter);
        return Task.FromResult(new PrinterStatus(printerName, true, "Çevrimiçi"));
    }

    public Task<IReadOnlyList<DiscoveredPrinter>> DiscoverAsync(CancellationToken ct)
    {
        var printers = new List<DiscoveredPrinter>();

        try
        {
            // winspool.drv EnumPrinters ile yerel yazıcıları keşfet
            const int PRINTER_ENUM_LOCAL = 0x00000002;
            EnumPrinters(PRINTER_ENUM_LOCAL, null!, 2,IntPtr.Zero, 0, out var needed, out _);

            if (needed > 0)
            {
                var buffer = Marshal.AllocHGlobal((int)needed);
                try
                {
                    if (EnumPrinters(PRINTER_ENUM_LOCAL, null!, 2,buffer, needed, out _, out var count))
                    {
                        var structSize = Marshal.SizeOf<PRINTER_INFO_2>();
                        for (int i = 0; i < count; i++)
                        {
                            var info = Marshal.PtrToStructure<PRINTER_INFO_2>(buffer + i * structSize);
                            if (info.pPrinterName is not null)
                                printers.Add(new DiscoveredPrinter(info.pPrinterName, "USB/Local", null, false));
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Windows yazıcı keşfi başarısız");
        }

        return Task.FromResult<IReadOnlyList<DiscoveredPrinter>>(printers);
    }

    #region winspool.drv P/Invoke

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
        [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
    }

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern int StartDocPrinter(IntPtr hPrinter, int level, ref DOCINFOA pDocInfo);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool EnumPrinters(int flags, string name, int level,
        IntPtr pPrinterEnum, uint cbBuf, out uint pcbNeeded, out uint pcReturned);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct PRINTER_INFO_2
    {
        public string pServerName;
        public string pPrinterName;
        public string pShareName;
        public string pPortName;
        public string pDriverName;
        public string pComment;
        public string pLocation;
        public IntPtr pDevMode;
        public string pSepFile;
        public string pPrintProcessor;
        public string pDatatype;
        public string pParameters;
        public IntPtr pSecurityDescriptor;
        public uint Attributes;
        public uint Priority;
        public uint DefaultPriority;
        public uint StartTime;
        public uint UntilTime;
        public uint Status;
        public uint cJobs;
        public uint AveragePPM;
    }

    #endregion
}

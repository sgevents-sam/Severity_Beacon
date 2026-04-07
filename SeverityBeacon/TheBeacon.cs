using System.IO.Ports;

namespace SeverityBeacon
{
    public class TheBeacon : IDisposable
    {
        private CancellationTokenSource? _CancellationToken;
        private readonly int _ClearBeaconAfter;
        private readonly string? _DeskLampState;
        private readonly string _DefaultState;
        private readonly SerialPort _Port;
        private readonly Action<string>? _StateChanged;
        private int _BlankAfter;
        private bool _Flashing;
        
        #region Initialisation
        public TheBeacon(string ComPort, string _defaultState, int clearBeaconAfter, string? deskLampState = null, Action<string>? stateChanged = null)
        {
            _Port = new SerialPort(ComPort, 9600);
            _ClearBeaconAfter = clearBeaconAfter;
            _DefaultState = _defaultState;
            _DeskLampState = deskLampState;
            _StateChanged = stateChanged;
            _Port.Open();
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            _Flashing = false;
            _CancellationToken?.Cancel();
            if( disposing )
            {
                _CancellationToken?.Dispose();
                _CancellationToken = null;
                _Port.Dispose();
            }
        }
        #endregion Initialisation

        #region Public Methods
        /// <summary>
        /// Return a list of available serial devices
        /// </summary>
        /// <returns></returns>
        public static List<string> GetBeaconDevices()
        {
            return SerialPort.GetPortNames().ToList();
        }

        /// <summary>
        /// Update the severity
        /// </summary>
        /// <param name="Option">The severity to display</param>
        public void SendBeaconIssue(SeverityOption? Option)
        {
            try
            {
                _Flashing = false;
                _CancellationToken?.Cancel();
                if( !_Port.IsOpen ) _Port.Open();
                if( Option == null && (_BlankAfter < _ClearBeaconAfter) )
                {
                    // State has recently changed to "all clear"
                    SetBeaconColour(_DefaultState);
                    _BlankAfter++;
                    return;
                }
                else if( Option == null && (_BlankAfter >= _ClearBeaconAfter) )
                {
                    // State has been all clear exceeding the intervals, clear the beacon or revert to desk lamp state
                    if (!string.IsNullOrWhiteSpace(_DeskLampState))
                    {
                        SetBeaconColour(_DeskLampState);
                    }
                    else
                    {
                        SetBeaconColour("#000000");
                    }

                    _BlankAfter++;
                    return;
                }
                
                // Set state 1
                _BlankAfter = 0;
                SetBeaconColour(Option!.BeaconHexColourState1);
                
                // Check if we have a second state for flashing
                if( !string.IsNullOrEmpty(Option.BeaconHexColourState2) && Option.BeaconChangeStateInterval1 != null )
                {
                    Task.Run(async () => await _RunBackgroundThread(Option));
                }
            }
            catch( Exception Ex )
            {
                Console.WriteLine($"Exception on BackgroundTask: {Ex.Message}");
                _Port.Close();
            }
        }
        #endregion Public Methods
        
        #region Private Methods
        public void SetBeaconColour(string hexCode)
        {
            _Flashing = false;
            _CancellationToken?.Cancel();
            WriteBeaconColour(hexCode);
        }

        /// <summary>
        /// Run the flashing task
        /// </summary>
        private async Task _RunBackgroundThread(SeverityOption Option)
        {
            try
            {
                _Flashing = true;
                if( !_Port.IsOpen ) _Port.Open();
                _CancellationToken = new CancellationTokenSource();
                while( _Flashing && Option.BeaconChangeStateInterval1 != null )
                {
                    WriteBeaconColour(Option.BeaconHexColourState1);
                    await Task.Delay(TimeSpan.FromMilliseconds((int)Option.BeaconChangeStateInterval1), _CancellationToken.Token);
                    
                    WriteBeaconColour(Option.BeaconHexColourState2!);
                    var StateChangeInterval2 = Option.BeaconChangeStateInterval2 ?? (int)Option.BeaconChangeStateInterval1;
                    await Task.Delay(TimeSpan.FromMilliseconds(StateChangeInterval2), _CancellationToken.Token);
                }
                
                _CancellationToken = null;
            }
            catch( TaskCanceledException ) {}
            catch( Exception Ex )
            {
                Console.WriteLine($"Exception on BackgroundTask: {Ex.Message}");
                _Port.Close();
            }
        }
        
        /// <summary>
        /// LED requires spacing between hex bytes, so convert #000000 to 00 00 00
        /// </summary>
        /// <param name="HexCode">Hex Code in #xxxxxx format</param>
        /// <returns>RGB Hex bytes seperated by spaces</returns>
        /// <exception cref="Exception">Invalid Hex Code</exception>
        private string _HexToByte(string HexCode)
        {
            if( HexCode.Length != 7 ) throw new Exception("Invalid hex code!");
            var hex1 = $"{HexCode[1]}{HexCode[2]}";
            var hex2 = $"{HexCode[3]}{HexCode[4]}";
            var hex3 = $"{HexCode[5]}{HexCode[6]}";
            if( hex1 == "00" ) hex1 = "01";
            if( hex2 == "00" ) hex2 = "01";
            if( hex3 == "00" ) hex3 = "01";
            return $"{hex1} {hex2} {hex3}";
        }

        private void WriteBeaconColour(string hexCode)
        {
            if (!_Port.IsOpen) _Port.Open();
            _Port.WriteLine($"WR 00 {_HexToByte(hexCode)}");
            _StateChanged?.Invoke(NormalizeHexCode(hexCode));
        }

        private static string NormalizeHexCode(string hexCode)
        {
            return hexCode.StartsWith('#') ? hexCode.ToUpperInvariant() : $"#{hexCode.ToUpperInvariant()}";
        }
        #endregion Private Methods
    }
}

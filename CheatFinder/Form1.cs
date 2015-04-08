using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace CheatFinder
{
    public partial class Form1 : Form
    {
        public string GenerateCheatCode(string[] dumps, string[] values, long newValue)
        {
            if (dumps == null || dumps.Length == 0)
                throw new ArgumentException("At least one dump file is required.");

            if (values == null || values.Length == 0)
                throw new ArgumentException("At least one value is required (one for each dump file).");

            List<byte[]> dumpData = new List<byte[]>();
            List<int[]> valuesSwapped = new List<int[]>(); 

            foreach (var dump in dumps)
            {
                if(!string.IsNullOrEmpty(dump))
                dumpData.Add(File.ReadAllBytes(dump));
            }

            if(dumpData.Count == 0)
                throw new ArgumentException("At least one dump file is required.");

            var valueLength = -1;
            var max = -1;
            foreach (var value in values)
            {
                long valueAsLong = 0;
                if (!Int64.TryParse(value, out valueAsLong))
                    throw new Exception(string.Format("'{0}' is an invalid value. Values should be decimal numbers.", value));

                valuesSwapped.Add(SwapEndianness(valueAsLong, out max));
                if (max > valueLength)
                    valueLength = max;
            }

            if (dumpData.Count != valuesSwapped.Count)
                throw new ArgumentException("Please specify a value (decimal) for each dump file.");

            var result = string.Empty;

            for (int i = 0; i < dumpData[0].Length; i++)
            {
                var offset = 0;
                foreach (var firstValueByte in valuesSwapped[0])
                {
                    if (i + offset < dumpData[0].Length && dumpData[0][i + offset] == firstValueByte)
                    {
                        if (offset == valueLength - 1)
                        {
                            var found = true;
                            if (dumpData.Count > 1)
                                found = SearchValuesAtAddress(i, valueLength, dumpData, valuesSwapped, 1);

                            if (found)
                            {
                                if (!string.IsNullOrEmpty(result))
                                    throw new Exception("More than one address with the specified value(s) has been found, please narrow down your search and/or verify the input data.");

                                result = string.Format("{0:X8} {1:X8}", i, newValue);
                            }

                        }
                    }
                    else
                        break;

                    offset++;
                }
            }

            return result;

            

        }

        private bool SearchValuesAtAddress(int address, int valueLength, List<byte[]> dumpData, List<int[]> values, int pos)
        {
            if (pos == dumpData.Count)
                return true;

            for (var i = 0; i < valueLength; i++ )
            {
                if (values[pos][i] == dumpData[pos][address + i])
                {
                    if (i == valueLength - 1)
                        return SearchValuesAtAddress(address, valueLength, dumpData, values, pos + 1);
                }
                else 
                    return false;
            }
            return false;
        }


        public Form1()
        {
            InitializeComponent();


            List<int> diffs = new List<int>();

            var data1 = File.ReadAllBytes("C:\\FCRAM-19.25.bin");
            var data2 = File.ReadAllBytes("C:\\FCRAM-19.22.bin");

            for (int i = 0; i < data1.Length; i++)
            {
                if(data1[i] != data2[i])
                {
                    diffs.Add(i);
                }
            }

        }




        public static int[] SwapEndianness(long value, out int length)
        {
            var array = new int[] { ((int)value & 0xFF), ((int)value & 0xFF00) >> 8, ((int)value & 0xFF0000) >> 16, (int)((value & 0xFF000000) >> 24) };
            length = (array[2] == 0 && array[3] == 0) ? 2 : 4;
            return array;
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {

            EnableControls(false, "Generating cheat code, please wait...");

            backgroundWorker1.RunWorkerAsync(new object[] {
                
                txtDumps.Lines,
                txtValues.Lines,
                (long)nudNewValue.Value
            });

            
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnBrowseDumps_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Please select one or more dumps";
            openFileDialog.Filter = "FCRAM Dump file|*.bin";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                StringBuilder builder = new StringBuilder();
                var index = 0;
                foreach (var filename in openFileDialog.FileNames)
                {
                    if (index > 0)
                        builder.Append("\r\n");
                    builder.AppendFormat("{0}", filename);
                    index++;
                }
                txtDumps.Text = builder.ToString();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var arguments = e.Argument as object[];
            e.Result = GenerateCheatCode(
                arguments[0] as string[], 
                arguments[1] as string[], 
                Convert.ToInt64(arguments[2]));
            
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                EnableControls(true, e.Error.Message);
            }
            else
            {
                txtCheatCode.Text = e.Result.ToString();
                if (!string.IsNullOrEmpty(txtCheatCode.Text))
                {
                    Clipboard.SetText(txtCheatCode.Text);
                    EnableControls(true, "Cheat code generated successfully.");
                    MessageBox.Show("The cheat code has been copied to your clipboard.", "Cheat code generated successfully", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    EnableControls(true, "Address not found.");
                    MessageBox.Show("No addresses with the specified values were found. Please try again.", "Address not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                }
            }
            
        }

        public void EnableControls(bool enabled, string status)
        {
            menuStrip1.Enabled = enabled;
            groupBox1.Enabled = enabled;
            groupBox2.Enabled = enabled;
            groupBox3.Enabled = enabled;
            groupBox4.Enabled = enabled;

            tslbStatus.Text = status;
            tspbProgress.Style = enabled ? ProgressBarStyle.Blocks : ProgressBarStyle.Marquee;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("By Raúl Bojalil \r\nhttp://raulbojalil.com\r\n\r\nMany thanks to the creators of ARCode. For more information please go to:\r\nhttp://gbatemp.net/threads/spider-arcode.383937/", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
         
        }

    }
}

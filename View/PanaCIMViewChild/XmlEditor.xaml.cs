using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SOCKETHUB.View.PanaCIMViewChild
{
    /// <summary>
    /// XmlEditor.xaml 的交互逻辑
    /// </summary>
    public partial class XmlEditor : Window
    {
        string _MsgType = null;
        public XmlEditor(string MsgType)
        {
            InitializeComponent();
            _MsgType = MsgType;
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            switch (_MsgType)
            {

                case "501": 
                    try
                    {
                        XMLMessageDetail.AppendText(System.Xml.Linq.XDocument.Parse(PANACIMVIEW.MsgStr501).ToString());
                    }
                    catch
                    {
                        XMLMessageDetail.AppendText(PANACIMVIEW.MsgStr501);
                    }
                    break;
                case "550":
                    try
                    {
                        XMLMessageDetail.AppendText(System.Xml.Linq.XDocument.Parse(PANACIMVIEW.MsgStr550).ToString());
                    }
                    catch
                    {
                        XMLMessageDetail.AppendText(PANACIMVIEW.MsgStr550);
                    }
                    break;
                case "551":
                    try
                    {
                        XMLMessageDetail.AppendText(System.Xml.Linq.XDocument.Parse(PANACIMVIEW.MsgStr551).ToString());
                    }
                    catch
                    {
                        XMLMessageDetail.AppendText(PANACIMVIEW.MsgStr551);
                    }
                    break;
                case "580":
                    try
                    {
                        XMLMessageDetail.AppendText(System.Xml.Linq.XDocument.Parse(PANACIMVIEW.MsgStr580).ToString());
                    }
                    catch
                    {
                        XMLMessageDetail.AppendText(PANACIMVIEW.MsgStr580);
                    }
                    break;
                case "Any":
                    try
                    {
                        XMLMessageDetail.AppendText(System.Xml.Linq.XDocument.Parse(PANACIMVIEW.MsgStrAny).ToString());
                    }
                    catch
                    {
                        XMLMessageDetail.AppendText(PANACIMVIEW.MsgStrAny);
                    }
                    break;
                default:
                    break;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            TextRange a = new TextRange(XMLMessageDetail.Document.ContentStart, XMLMessageDetail.Document.ContentEnd);
            switch (_MsgType)
            {
                case "501":
                    PANACIMVIEW.MsgStr501 = a.Text;
                    break;
                case "550":
                    PANACIMVIEW.MsgStr550 = a.Text;
                    break;
                case "551":
                    PANACIMVIEW.MsgStr551 = a.Text;
                    break;
                case "580":
                    PANACIMVIEW.MsgStr580 = a.Text;
                    break;
                case "Any":
                    PANACIMVIEW.MsgStrAny = a.Text;
                    break;
                default:
                    break;
            }
        }

    }
}

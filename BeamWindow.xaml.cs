﻿using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Web;
using System.Web.Script.Serialization;
using Beam.oAuth;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text;
using System.Collections.Generic;
using Beam.Helper;
using static Beam.Helper.Extension;
using Beam.View;
using System.Windows.Threading;

namespace Beam
{
    /// <summary>
    /// Interaction logic for BeamWindow.xaml
    /// </summary>
    /// 
    public partial class BeamWindow : Window
    {

        public Twitter t = new Twitter();
        string _currentView = null;


        Dictionary<String, UserControl> viewDict = new Dictionary<string, UserControl>();

        public BeamWindow()
        {
            InitializeComponent();

            viewDict.Add("init", new InitAuthView());
            viewDict.Add("auth", new TryAuthView());
            viewDict.Add("timeline", new TimelineView());

            ChangeView("init");
            
            if (!String.IsNullOrEmpty(Properties.Settings.Default.token) || !String.IsNullOrEmpty(Properties.Settings.Default.tokenSec))
            {
                ChangeView("timeline");
                rdMenu.Height = new GridLength(32);
                Task.Run(async () => await startStream());
            }
            
            btTimeline.Click += delegate { ChangeView("timeline"); };
            btConnect.Click += delegate { ChangeView("auth"); };
            btMessage.Click += delegate { ChangeView("init"); };
        }

        public async Task startStream()
        {
            await t.singleUserStream(delegate (string json)
            {
                JavaScriptSerializer jss = new JavaScriptSerializer();
                dynamic tweet = jss.Deserialize<dynamic>(System.Net.WebUtility.HtmlDecode(json));
                TweetType type = checkTweetType(tweet);
                TweetPanel panel = new TweetPanel();
                switch (type)
                {

                    case TweetType.Normal:
                        panel.ID = (long)tweet["id"];
                        panel.Username = String.Format("{0}/{1}", tweet["user"]["screen_name"], tweet["user"]["name"]);
                        panel.Text = tweet["text"];
                        panel.ProfileImage = tweet["user"]["profile_image_url_https"];
                        panel.TimestampWithClient = String.Format("{0} / via {1}", Extension.ParseDatetime(tweet["created_at"]), Extension.ParseClientSource(tweet["source"]));

                        //((TimelineView)viewDict["timeline"]).InsertTweet();// listTweet.Items.Insert(0, panel);
                        break;
                    case TweetType.Message:
                        panel.ID = (long)tweet["direct_message"]["id"];
                        panel.Username = String.Format("{0}/{1}", tweet["user"]["screen_name"], tweet["user"]["name"]);
                        panel.Text = tweet["direct_message"]["text"];
                        panel.ProfileImage = tweet["direct_message"]["sender"]["profile_image_url_https"];
                        panel.TimestampWithClient = String.Format("{0}", Extension.ParseDatetime(tweet["direct_message"]["created_at"]));
                        //listDM.Items.Insert(0, panel);
                        break;
                }

                //CurrentDispatcher.BeginInvoke((Action)(() => { }));
            }, CurrentDispatcher.BeginInvoke((Action)(() =>
            {
                AlertBox alertBox = new AlertBox();
                alertBox.SetMessage("Unable to connect to userstream");
                alertBox.MouseUp += delegate { ErrorStack.Children.Remove(alertBox); alertBox = null; };
                ErrorStack.Children.Add(alertBox);
            })));
        }

        /*
        private void tbTweet_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                int carIndex = tbTweet.CaretIndex+1;
                tbTweet.Text = tbTweet.Text.Insert(tbTweet.CaretIndex, "\n");
                tbTweet.Select(carIndex, 0);
                return;
            }
            if (e.Key == Key.Enter && !String.IsNullOrWhiteSpace(tbTweet.Text))
            {
                t.oAuthWebRequest(Twitter.Method.POST, "https://api.twitter.com/1.1/statuses/update.json", "status=" + oAuth.UrlEncode(tbTweet.Text));
                tbTweet.Text = null;
            }
        }
        */

        public void ChangeView(string viewKey)
        {
            if (viewKey != _currentView && viewDict.ContainsKey(viewKey))
            {
                ContentWrapper.Content = viewDict[viewKey];
                pTimeline.Fill = pConnect.Fill = pMessage.Fill = new SolidColorBrush(Color.FromRgb(204, 214, 221));
                switch (viewKey)
                {
                    case "init":
                    case "auth":
                        rdMenu.Height = new GridLength(0);
                        return;
                    case "timeline":
                        pTimeline.Fill = new SolidColorBrush(Color.FromRgb(85, 172, 238));
                        return;
                    case "connect":
                        pConnect.Fill = new SolidColorBrush(Color.FromRgb(85, 172, 238));
                        return;
                    case "message":
                        pMessage.Fill = new SolidColorBrush(Color.FromRgb(85, 172, 238));
                        return;
                }
                
            }

        }

        Dispatcher CurrentDispatcher
        {
            get
            {
                if (Application.Current != null)
                    return Application.Current.Dispatcher;
                else
                    return Dispatcher.CurrentDispatcher;
            }
        }
    }
}

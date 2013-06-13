using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using Zeta;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.CommonBot;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Profile.Common;
using Zeta.XmlEngine;
using Zeta.TreeSharp;
using Zeta.Internals;
using Zeta.Internals.Actors;

namespace AutoEquipper
{
    public partial class AutoEquipper : IPlugin
    {
        private Button saveButton;
        private CheckBox checkCheckStash, checkDisable60, checkBuyPots, checkIdentifyItems, checkIgnoreHead, checkIgnoreShoulders, checkIgnoreTorso, checkIgnoreHands, checkIgnoreWrists, checkIgnoreWaist, checkIgnoreLegs, checkIgnoreFeet, checkIgnoreNeck, checkIgnoreFingerL, checkIgnoreFingerR, checkIgnoreHand, checkIgnoreOffhand;
		private TextBox damageFactor, ehpFactor;
		private Slider sldPotion1, sldPotion2, sldPotion3, sldPotion4, sldPotion5, sldPotion6, sldPotion7, sldPotion8, sldPotion9, sldPotion10;
		
        private Window configWindow = null;
        public Window DisplayWindow
        {
            get
            {
                if (!File.Exists(pluginPath + "UI\\ConfigWindow.xaml"))
                    Log("ERROR: Can't find \"" + pluginPath + "UI\\ConfigWindow.xaml\"");
                try
                {
                    if (configWindow == null)
                    {
                        configWindow = new Window();
                    }
                    StreamReader xamlStream = new StreamReader(pluginPath + "UI\\ConfigWindow.xaml");
                    DependencyObject xamlContent = System.Windows.Markup.XamlReader.Load(xamlStream.BaseStream) as DependencyObject;
                    configWindow.Content = xamlContent;

					checkIgnoreHead = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkIgnoreHead") as CheckBox;
					checkIgnoreHead.Checked += new RoutedEventHandler(checkIgnoreHead_check);
					checkIgnoreHead.Unchecked += new RoutedEventHandler(checkIgnoreHead_uncheck);
					
					checkIgnoreShoulders = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkIgnoreShoulders") as CheckBox;
					checkIgnoreShoulders.Checked += new RoutedEventHandler(checkIgnoreShoulders_check);
					checkIgnoreShoulders.Unchecked += new RoutedEventHandler(checkIgnoreShoulders_uncheck);

					checkIgnoreTorso = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkIgnoreTorso") as CheckBox;
					checkIgnoreTorso.Checked += new RoutedEventHandler(checkIgnoreTorso_check);
					checkIgnoreTorso.Unchecked += new RoutedEventHandler(checkIgnoreTorso_uncheck);
	
					checkIgnoreHands = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkIgnoreHands") as CheckBox;
					checkIgnoreHands.Checked += new RoutedEventHandler(checkIgnoreHands_check);
					checkIgnoreHands.Unchecked += new RoutedEventHandler(checkIgnoreHands_uncheck);

					checkIgnoreWrists = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkIgnoreWrists") as CheckBox;
					checkIgnoreWrists.Checked += new RoutedEventHandler(checkIgnoreWrists_check);
					checkIgnoreWrists.Unchecked += new RoutedEventHandler(checkIgnoreWrists_uncheck);
		
					checkIgnoreWaist = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkIgnoreWaist") as CheckBox;
					checkIgnoreWaist.Checked += new RoutedEventHandler(checkIgnoreWaist_check);
					checkIgnoreWaist.Unchecked += new RoutedEventHandler(checkIgnoreWaist_uncheck);

					checkIgnoreLegs = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkIgnoreLegs") as CheckBox;
					checkIgnoreLegs.Checked += new RoutedEventHandler(checkIgnoreLegs_check);
					checkIgnoreLegs.Unchecked += new RoutedEventHandler(checkIgnoreLegs_uncheck);

					checkIgnoreFeet = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkIgnoreFeet") as CheckBox;
					checkIgnoreFeet.Checked += new RoutedEventHandler(checkIgnoreFeet_check);
					checkIgnoreFeet.Unchecked += new RoutedEventHandler(checkIgnoreFeet_uncheck);

					checkIgnoreNeck = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkIgnoreNeck") as CheckBox;
					checkIgnoreNeck.Checked += new RoutedEventHandler(checkIgnoreNeck_check);
					checkIgnoreNeck.Unchecked += new RoutedEventHandler(checkIgnoreNeck_uncheck);

					checkIgnoreFingerL = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkIgnoreFingerL") as CheckBox;
					checkIgnoreFingerL.Checked += new RoutedEventHandler(checkIgnoreFingerL_check);
					checkIgnoreFingerL.Unchecked += new RoutedEventHandler(checkIgnoreFingerL_uncheck);

					checkIgnoreFingerR = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkIgnoreFingerR") as CheckBox;
					checkIgnoreFingerR.Checked += new RoutedEventHandler(checkIgnoreFingerR_check);
					checkIgnoreFingerR.Unchecked += new RoutedEventHandler(checkIgnoreFingerR_uncheck);

					checkIgnoreHand = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkIgnoreHand") as CheckBox;
					checkIgnoreHand.Checked += new RoutedEventHandler(checkIgnoreHand_check);
					checkIgnoreHand.Unchecked += new RoutedEventHandler(checkIgnoreHand_uncheck);

					checkIgnoreOffhand = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkIgnoreOffhand") as CheckBox;
					checkIgnoreOffhand.Checked += new RoutedEventHandler(checkIgnoreOffhand_check);
					checkIgnoreOffhand.Unchecked += new RoutedEventHandler(checkIgnoreOffhand_uncheck);
					
                    checkIdentifyItems = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkIdentifyItems") as CheckBox;
                    checkIdentifyItems.Checked += new RoutedEventHandler(checkIdentifyItems_check);
                    checkIdentifyItems.Unchecked += new RoutedEventHandler(checkIdentifyItems_uncheck);
					
                    checkBuyPots = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkBuyPots") as CheckBox;
                    checkBuyPots.Checked += new RoutedEventHandler(checkBuyPots_check);
                    checkBuyPots.Unchecked += new RoutedEventHandler(checkBuyPots_uncheck);
					
                    checkDisable60 = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkDisable60") as CheckBox;
                    checkDisable60.Checked += new RoutedEventHandler(checkDisable60_check);
                    checkDisable60.Unchecked += new RoutedEventHandler(checkDisable60_uncheck);
                    
                    checkCheckStash = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkCheckStash") as CheckBox;
                    checkCheckStash.Checked += new RoutedEventHandler(checkCheckStash_check);
                    checkCheckStash.Unchecked += new RoutedEventHandler(checkCheckStash_uncheck);
                    
                    damageFactor = LogicalTreeHelper.FindLogicalNode(xamlContent, "DamageFactor") as TextBox;
                    damageFactor.LostFocus += new RoutedEventHandler(updateDamageFactor);

                    ehpFactor = LogicalTreeHelper.FindLogicalNode(xamlContent, "EHPFactor") as TextBox;
                    ehpFactor.LostFocus += new RoutedEventHandler(updateEHPFactor);
					
					sldPotion1 = LogicalTreeHelper.FindLogicalNode(xamlContent, "sldPotion1") as Slider;
					sldPotion1.ValueChanged += new RoutedPropertyChangedEventHandler<double>(sldPotion1_ValueChanged);
					
					sldPotion2 = LogicalTreeHelper.FindLogicalNode(xamlContent, "sldPotion2") as Slider;
					sldPotion2.ValueChanged += new RoutedPropertyChangedEventHandler<double>(sldPotion2_ValueChanged);
					
					sldPotion3 = LogicalTreeHelper.FindLogicalNode(xamlContent, "sldPotion3") as Slider;
					sldPotion3.ValueChanged += new RoutedPropertyChangedEventHandler<double>(sldPotion3_ValueChanged);
					
					sldPotion4 = LogicalTreeHelper.FindLogicalNode(xamlContent, "sldPotion4") as Slider;
					sldPotion4.ValueChanged += new RoutedPropertyChangedEventHandler<double>(sldPotion4_ValueChanged);
					
					sldPotion5 = LogicalTreeHelper.FindLogicalNode(xamlContent, "sldPotion5") as Slider;
					sldPotion5.ValueChanged += new RoutedPropertyChangedEventHandler<double>(sldPotion5_ValueChanged);
					
					sldPotion6 = LogicalTreeHelper.FindLogicalNode(xamlContent, "sldPotion6") as Slider;
					sldPotion6.ValueChanged += new RoutedPropertyChangedEventHandler<double>(sldPotion6_ValueChanged);
					
					sldPotion7 = LogicalTreeHelper.FindLogicalNode(xamlContent, "sldPotion7") as Slider;
					sldPotion7.ValueChanged += new RoutedPropertyChangedEventHandler<double>(sldPotion7_ValueChanged);
					
					sldPotion8 = LogicalTreeHelper.FindLogicalNode(xamlContent, "sldPotion8") as Slider;
					sldPotion8.ValueChanged += new RoutedPropertyChangedEventHandler<double>(sldPotion8_ValueChanged);
					
					sldPotion9 = LogicalTreeHelper.FindLogicalNode(xamlContent, "sldPotion9") as Slider;
					sldPotion9.ValueChanged += new RoutedPropertyChangedEventHandler<double>(sldPotion9_ValueChanged);
					
					sldPotion10 = LogicalTreeHelper.FindLogicalNode(xamlContent, "sldPotion10") as Slider;
					sldPotion10.ValueChanged += new RoutedPropertyChangedEventHandler<double>(sldPotion10_ValueChanged);
					
                    saveButton = LogicalTreeHelper.FindLogicalNode(xamlContent, "buttonSave") as Button;
                    saveButton.Click += new RoutedEventHandler(buttonSave_Click);

                    UserControl mainControl = LogicalTreeHelper.FindLogicalNode(xamlContent, "mainControl") as UserControl;
                    // Set height and width to main window
                    configWindow.Height = mainControl.Height + 30;
                    configWindow.Width = mainControl.Width;
                    configWindow.Title = "Auto Equipper";

                    // On load example
                    configWindow.Loaded += new RoutedEventHandler(configWindow_Loaded);
                    configWindow.Closed += configWindow_Closed;

                    // Add our content to our main window
                    configWindow.Content = xamlContent;
                }
                catch (System.Windows.Markup.XamlParseException ex)
                {
                    // You can get specific error information like LineNumber from the exception
                    Log(ex.ToString());
                }
                catch (Exception ex)
                {
                    // Some other error
                    Log(ex.ToString());
                }
                return configWindow;
            }
        }	
		
		private void checkIgnoreHead_check(object sender, RoutedEventArgs e) 		{ bIgnoreHead = true; }
		private void checkIgnoreHead_uncheck(object sender, RoutedEventArgs e) 		{ bIgnoreHead = false; }
		private void checkIgnoreShoulders_check(object sender, RoutedEventArgs e) 	{ bIgnoreShoulders = true; }
		private void checkIgnoreShoulders_uncheck(object sender, RoutedEventArgs e) { bIgnoreShoulders = false; }
		private void checkIgnoreTorso_check(object sender, RoutedEventArgs e) 		{ bIgnoreTorso = true; }
		private void checkIgnoreTorso_uncheck(object sender, RoutedEventArgs e) 	{ bIgnoreTorso = false; }
		private void checkIgnoreHands_check(object sender, RoutedEventArgs e) 		{ bIgnoreHands = true; }
		private void checkIgnoreHands_uncheck(object sender, RoutedEventArgs e) 	{ bIgnoreHands = false; }
		private void checkIgnoreWrists_check(object sender, RoutedEventArgs e) 		{ bIgnoreWrists = true; }
		private void checkIgnoreWrists_uncheck(object sender, RoutedEventArgs e) 	{ bIgnoreWrists = false; }
		private void checkIgnoreWaist_check(object sender, RoutedEventArgs e) 		{ bIgnoreWaist = true; }
		private void checkIgnoreWaist_uncheck(object sender, RoutedEventArgs e) 	{ bIgnoreWaist = false; }
		private void checkIgnoreLegs_check(object sender, RoutedEventArgs e) 		{ bIgnoreLegs = true; }
		private void checkIgnoreLegs_uncheck(object sender, RoutedEventArgs e) 		{ bIgnoreLegs = false; }
		private void checkIgnoreFeet_check(object sender, RoutedEventArgs e) 		{ bIgnoreFeet = true; }
		private void checkIgnoreFeet_uncheck(object sender, RoutedEventArgs e) 		{ bIgnoreFeet = false; }
		private void checkIgnoreNeck_check(object sender, RoutedEventArgs e) 		{ bIgnoreNeck = true; }
		private void checkIgnoreNeck_uncheck(object sender, RoutedEventArgs e) 		{ bIgnoreNeck = false; }
		private void checkIgnoreFingerL_check(object sender, RoutedEventArgs e) 	{ bIgnoreFingerL = true; }
		private void checkIgnoreFingerL_uncheck(object sender, RoutedEventArgs e) 	{ bIgnoreFingerL = false; }
		private void checkIgnoreFingerR_check(object sender, RoutedEventArgs e) 	{ bIgnoreFingerR = true; }
		private void checkIgnoreFingerR_uncheck(object sender, RoutedEventArgs e) 	{ bIgnoreFingerR = false; }
		private void checkIgnoreHand_check(object sender, RoutedEventArgs e) 		{ bIgnoreHand = true; }
		private void checkIgnoreHand_uncheck(object sender, RoutedEventArgs e) 		{ bIgnoreHand = false; }
		private void checkIgnoreOffhand_check(object sender, RoutedEventArgs e) 	{ bIgnoreOffhand = true; }
		private void checkIgnoreOffhand_uncheck(object sender, RoutedEventArgs e) 	{ bIgnoreOffhand = false; }
        private void checkIdentifyItems_check(object sender, RoutedEventArgs e) 	{ bIdentifyItems = true; }
        private void checkIdentifyItems_uncheck(object sender, RoutedEventArgs e) 	{ bIdentifyItems = false; }
        private void checkBuyPots_check(object sender, RoutedEventArgs e) 			{ bBuyPots = true; }
        private void checkBuyPots_uncheck(object sender, RoutedEventArgs e) 		{ bBuyPots = false; }
        private void checkDisable60_check(object sender, RoutedEventArgs e) 		{ bDisable60 = true; }
        private void checkDisable60_uncheck(object sender, RoutedEventArgs e) 		{ bDisable60 = false; }
        private void checkCheckStash_check(object sender, RoutedEventArgs e) 		{ bCheckStash = true; }
        private void checkCheckStash_uncheck(object sender, RoutedEventArgs e) 		{ bCheckStash = false; }
		private void sldPotion1_ValueChanged(object sender, RoutedEventArgs e)
		{
            Slider slider = e.OriginalSource as Slider;
            if (slider != null) iQtyPotion1 = slider.Value; 
		}
		private void sldPotion2_ValueChanged(object sender, RoutedEventArgs e)
		{
            Slider slider = e.OriginalSource as Slider;
            if (slider != null) iQtyPotion2 = slider.Value; 
		}
		private void sldPotion3_ValueChanged(object sender, RoutedEventArgs e)
		{
            Slider slider = e.OriginalSource as Slider;
            if (slider != null) iQtyPotion3 = slider.Value; 
		}
		private void sldPotion4_ValueChanged(object sender, RoutedEventArgs e)
		{
            Slider slider = e.OriginalSource as Slider;
            if (slider != null) iQtyPotion4 = slider.Value; 
		}
		private void sldPotion5_ValueChanged(object sender, RoutedEventArgs e)
		{
            Slider slider = e.OriginalSource as Slider;
            if (slider != null) iQtyPotion5 = slider.Value; 
		}
		private void sldPotion6_ValueChanged(object sender, RoutedEventArgs e)
		{
            Slider slider = e.OriginalSource as Slider;
            if (slider != null) iQtyPotion6 = slider.Value; 
		}
		private void sldPotion7_ValueChanged(object sender, RoutedEventArgs e)
		{
            Slider slider = e.OriginalSource as Slider;
            if (slider != null) iQtyPotion7 = slider.Value; 
		}
		private void sldPotion8_ValueChanged(object sender, RoutedEventArgs e)
		{
            Slider slider = e.OriginalSource as Slider;
            if (slider != null) iQtyPotion8 = slider.Value; 
		}
		private void sldPotion9_ValueChanged(object sender, RoutedEventArgs e)
		{
            Slider slider = e.OriginalSource as Slider;
            if (slider != null) iQtyPotion9 = slider.Value; 
		}
		private void sldPotion10_ValueChanged(object sender, RoutedEventArgs e)
		{
            Slider slider = e.OriginalSource as Slider;
            if (slider != null) iQtyPotion10 = slider.Value; 
		}
		
        private void configWindow_Closed(object sender, EventArgs e)
        {
            configWindow = null;
        }
		
        private void configWindow_Loaded(object sender, RoutedEventArgs e)
        {
			checkIgnoreHead.IsChecked = (bIgnoreHead) ? true : false;
			checkIgnoreShoulders.IsChecked = (bIgnoreShoulders) ? true : false;
			checkIgnoreTorso.IsChecked = (bIgnoreTorso) ? true : false;
			checkIgnoreHands.IsChecked = (bIgnoreHands) ? true : false;
			checkIgnoreWrists.IsChecked = (bIgnoreWrists) ? true : false;
			checkIgnoreWaist.IsChecked = (bIgnoreWaist) ? true : false;
			checkIgnoreLegs.IsChecked = (bIgnoreLegs) ? true : false;
			checkIgnoreFeet.IsChecked = (bIgnoreFeet) ? true : false;
			checkIgnoreNeck.IsChecked = (bIgnoreNeck) ? true : false;
			checkIgnoreFingerL.IsChecked = (bIgnoreFingerL) ? true : false;
			checkIgnoreFingerR.IsChecked = (bIgnoreFingerR) ? true : false;
			checkIgnoreHand.IsChecked = (bIgnoreHand) ? true : false;
			checkIgnoreOffhand.IsChecked = (bIgnoreOffhand) ? true : false;
			checkIdentifyItems.IsChecked = (bIdentifyItems) ? true : false;
			checkBuyPots.IsChecked = (bBuyPots) ? true : false;
			checkDisable60.IsChecked = (bDisable60) ? true : false;
			checkCheckStash.IsChecked = (bCheckStash) ? true : false;
			ehpFactor.Text = EHPFactor.ToString();
            damageFactor.Text = DamageFactor.ToString();
			sldPotion1.Value = iQtyPotion1;
			sldPotion2.Value = iQtyPotion2;
			sldPotion3.Value = iQtyPotion3;
			sldPotion4.Value = iQtyPotion4;
			sldPotion5.Value = iQtyPotion5;
			sldPotion6.Value = iQtyPotion6;
			sldPotion7.Value = iQtyPotion7;
			sldPotion8.Value = iQtyPotion8;
			sldPotion9.Value = iQtyPotion9;
			sldPotion10.Value = iQtyPotion10;
        }
		
        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveConfiguration();
            configWindow.Close();
			LastFullEvaluation.Restart();
			bNeedFullItemUpdate = true;
        }

        private void SaveConfiguration()
        {
            if (bSavingConfig) return;
            bSavingConfig = true;
            FileStream configStream = File.Open(sConfigFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            using (StreamWriter configWriter = new StreamWriter(configStream))
            {
				configWriter.WriteLine("bIgnoreHead=" 		+ (bIgnoreHead.ToString()).ToLower());
				configWriter.WriteLine("bIgnoreShoulders=" 	+ (bIgnoreShoulders.ToString()).ToLower());
				configWriter.WriteLine("bIgnoreTorso=" 		+ (bIgnoreTorso.ToString()).ToLower());
				configWriter.WriteLine("bIgnoreHands=" 		+ (bIgnoreHands.ToString()).ToLower());
				configWriter.WriteLine("bIgnoreWrists=" 	+ (bIgnoreWrists.ToString()).ToLower());
				configWriter.WriteLine("bIgnoreWaist=" 		+ (bIgnoreWaist.ToString()).ToLower());
				configWriter.WriteLine("bIgnoreLegs=" 		+ (bIgnoreLegs.ToString()).ToLower());
				configWriter.WriteLine("bIgnoreFeet=" 		+ (bIgnoreFeet.ToString()).ToLower());
				configWriter.WriteLine("bIgnoreNeck=" 		+ (bIgnoreNeck.ToString()).ToLower());
				configWriter.WriteLine("bIgnoreFingerL=" 	+ (bIgnoreFingerL.ToString()).ToLower());
				configWriter.WriteLine("bIgnoreFingerR=" 	+ (bIgnoreFingerR.ToString()).ToLower());
				configWriter.WriteLine("bIgnoreHand=" 		+ (bIgnoreHand.ToString()).ToLower());
				configWriter.WriteLine("bIgnoreOffhand=" 	+ (bIgnoreOffhand.ToString()).ToLower());
				configWriter.WriteLine("bIdentifyItems=" 	+ (bIdentifyItems.ToString()).ToLower());
				configWriter.WriteLine("bBuyPots=" 			+ (bBuyPots.ToString()).ToLower());
				configWriter.WriteLine("bDisable60=" 		+ (bDisable60.ToString()).ToLower());
				configWriter.WriteLine("bCheckStash=" 		+ (bCheckStash.ToString()).ToLower());
				configWriter.WriteLine("damageFactor=" 		+ DamageFactor.ToString());
				configWriter.WriteLine("ehpFactor=" 		+ EHPFactor.ToString());
				configWriter.WriteLine("iQtyPotion1=" 		+ iQtyPotion1.ToString());
				configWriter.WriteLine("iQtyPotion2=" 		+ iQtyPotion2.ToString());
				configWriter.WriteLine("iQtyPotion3=" 		+ iQtyPotion3.ToString());
				configWriter.WriteLine("iQtyPotion4=" 		+ iQtyPotion4.ToString());
				configWriter.WriteLine("iQtyPotion5=" 		+ iQtyPotion5.ToString());
				configWriter.WriteLine("iQtyPotion6=" 		+ iQtyPotion6.ToString());
				configWriter.WriteLine("iQtyPotion7=" 		+ iQtyPotion7.ToString());
				configWriter.WriteLine("iQtyPotion8=" 		+ iQtyPotion8.ToString());
				configWriter.WriteLine("iQtyPotion9=" 		+ iQtyPotion9.ToString());
				configWriter.WriteLine("iQtyPotion10=" 		+ iQtyPotion10.ToString());
            }
            configStream.Close();
            bSavingConfig = false;
        }

        private void LoadConfiguration()
        {
            //Check for Config file
            if (!File.Exists(sConfigFile))
            {
                Log("No config file found, now creating a new config from defaults at: " + sConfigFile);
                SaveConfiguration();
                return;
            }
            //Load File
            using (StreamReader configReader = new StreamReader(sConfigFile))
            {
                while (!configReader.EndOfStream)
                {
                    string[] config = configReader.ReadLine().Split('=');
                    if (config != null)
                    {
                        switch (config[0])
                        {
							case "bIgnoreHead":
                                bIgnoreHead = Convert.ToBoolean(config[1]);
                                break;
							case "bIgnoreShoulders":
                                bIgnoreShoulders = Convert.ToBoolean(config[1]);
                                break;
							case "bIgnoreTorso":
                                bIgnoreTorso = Convert.ToBoolean(config[1]);
                                break;
							case "bIgnoreHands":
                                bIgnoreHands = Convert.ToBoolean(config[1]);
                                break;
							case "bIgnoreWrists":
                                bIgnoreWrists = Convert.ToBoolean(config[1]);
                                break;
							case "bIgnoreWaist":
                                bIgnoreWaist = Convert.ToBoolean(config[1]);
                                break;
							case "bIgnoreLegs":
                                bIgnoreLegs = Convert.ToBoolean(config[1]);
                                break;
							case "bIgnoreFeet":
                                bIgnoreFeet = Convert.ToBoolean(config[1]);
                                break;
							case "bIgnoreNeck":
                                bIgnoreNeck = Convert.ToBoolean(config[1]);
                                break;
							case "bIgnoreFingerL":
                                bIgnoreFingerL = Convert.ToBoolean(config[1]);
                                break;
							case "bIgnoreFingerR":
                                bIgnoreFingerR = Convert.ToBoolean(config[1]);
                                break;
							case "bIgnoreHand":
                                bIgnoreHand = Convert.ToBoolean(config[1]);
                                break;
							case "bIgnoreOffhand":
                                bIgnoreOffhand = Convert.ToBoolean(config[1]);
                                break;
                            case "bIdentifyItems":
                                bIdentifyItems = Convert.ToBoolean(config[1]);
                                break;
                            case "bBuyPots":
                                bBuyPots = Convert.ToBoolean(config[1]);
                                break;
                            case "bDisable60":
                                bDisable60 = Convert.ToBoolean(config[1]);
                                break;
							case "bCheckStash":
                                bCheckStash = Convert.ToBoolean(config[1]);
                                break;
                            case "damageFactor":
                                DamageFactor = Convert.ToInt32(config[1]);
                                break;
                            case "ehpFactor":
                                EHPFactor = Convert.ToInt32(config[1]);
                                break;
							case "iQtyPotion1": // 1-5, Minor Health Potion (1)
								iQtyPotion1 = Convert.ToInt32(config[1]);
                                break;
							case "iQtyPotion2": // 6-10, Lesser Health Potion (10)
								iQtyPotion2 = Convert.ToInt32(config[1]);
                                break;
							case "iQtyPotion3": // 11-15, Health Potion (15)
								iQtyPotion3 = Convert.ToInt32(config[1]);
                                break;
							case "iQtyPotion4": // 16-20, Greater Health Potion (20)
								iQtyPotion4 = Convert.ToInt32(config[1]);
                                break;
							case "iQtyPotion5": // 21-25, Major Health Potion (25)
								iQtyPotion5 = Convert.ToInt32(config[1]);
                                break;
							case "iQtyPotion6": // 26-36, Super Health Potion (30)
								iQtyPotion6 = Convert.ToInt32(config[1]);
                                break;
							case "iQtyPotion7": // 37-46, Heroic Health Potion (40)
								iQtyPotion7 = Convert.ToInt32(config[1]);
                                break;
							case "iQtyPotion8": // 47-52, Resplendent Health Potion (50)
								iQtyPotion8 = Convert.ToInt32(config[1]);
                                break;
							case "iQtyPotion9": // 53-57, Runic Health Potion (55)
								iQtyPotion9 = Convert.ToInt32(config[1]);
                                break;
							case "iQtyPotion10": // 58+, Mythic Health Potion (60)
								iQtyPotion10 = Convert.ToInt32(config[1]);
                                break;
                        }
                    }
                }
                configReader.Close();
            }
        }
		
	}
}
using System;
using System.Collections.Generic;
using System.Text;
using SpaceControl.Entities;
using XNA_GUI.GUIElements;
using System.Xml;

namespace SpaceControl.Utility
{
    public static class SelectedManager
    {
        private static Planet s_previousSelected = null;
        private static GUI_Base s_root = null;

        public static Planet Selected
        {
            get { return s_previousSelected; }
        }
        #region XML Helper Strings
        private const string c_sliderName = "Deployment Route {0}";
        private const string c_sliderXMLTemplate = "<ScrollBar Name=\"Deployment Route {0}\"><ControlSize Height=\"3\" Width=\"60\"/> " +
           "<ControlPosition X=\"5\" Y=\"{1}\"/><SliderBarImage Type=\"File\" Name=\"SliderBar.png\"/>" +
           "<SliderImage Type=\"File\" Name=\"SliderButton.png\"/><MinValue>0</MinValue><MaxValue>100</MaxValue>" +
           "<InitialValue>{2}</InitialValue><Orientation>Horizontal</Orientation></ScrollBar>";

        private const string c_cancelRouteButton = "<Button Name=\"Cancel Button {0}\"><ControlSize Height=\"3\" Width=\"10\"/>" +
            "<ControlPosition X=\"75\" Y=\"{1}\"/><OnClick>CancelRoute</OnClick><DisplayText>X</DisplayText></Button>";

        private const string c_TestLabel = "<TextLabel Name=\"Label {0}\"><ControlSize Height=\"3\" Width =\"80\"/>" +
            "<ControlPosition X=\"10\" Y=\"{1}\"/><DisplayText>{2}</DisplayText>{2}</TextLabel>";

        #endregion XML Helper Strings

        private const int c_SliderSpacingPercent = 8;
        private static List<SliderBar> s_sliderPool = new List<SliderBar>(1);

        public static void MakeSelected(Planet picked, GUI_Base root, Player viewing)
        {

            //If required, remove the existing routes.
            //this only happens when we move from one planet selected to another.
            if (s_previousSelected != null)
                ClearSelected(root);
            s_root = root;
            s_previousSelected = picked;

            //How much information is the player going to be able to see?
            bool playerControledPlanet = viewing.ControlsPlanet(picked);
            bool visibleToPlayer;
            if (playerControledPlanet == true)
                visibleToPlayer = true;
            else
                visibleToPlayer = viewing.CanSeePlanet(picked);

            TextLabel planetName = (TextLabel)root.GetChildByName("Planet Name");
            TextLabel defenseLabel = (TextLabel)root.GetChildByName("Defense Fleets");
            TextLabel production = (TextLabel)root.GetChildByName("Production");

            planetName.DisplayText = picked.Name;
            defenseLabel.DisplayText = "Deffense Fleets: " + picked.DefenseFleets.ToString();

            if (visibleToPlayer == true)
                production.DisplayText = string.Format("Production: {0} per min", (int)picked.Production);
            else
                production.DisplayText = "No Data Available";

            if (playerControledPlanet == true)
            {
                List<float> depolymentRates = picked.DispatchRates;
                for (int i = 0; i < depolymentRates.Count; i++)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(string.Format(c_sliderXMLTemplate, i, 40 + (c_SliderSpacingPercent * i), (int)(100 * depolymentRates[i])));
                    SliderBar s = new SliderBar(doc.GetElementsByTagName("ScrollBar")[0], root, null);
                    root.AddChild(s);
                    if (i != 0)
                    {
                        doc.LoadXml(string.Format(c_cancelRouteButton, i, 40 + (c_SliderSpacingPercent * i)));
                        Button b = new Button(doc.GetElementsByTagName("Button")[0], root, picked);
                        root.AddChild(b);

                        doc.LoadXml(string.Format(c_TestLabel, i, 40 + (c_SliderSpacingPercent * i) - 3, picked.DeploymentRoutes[i-1].Desitnation.Name));
                        TextLabel label = new TextLabel(doc.GetElementsByTagName("TextLabel")[0], root, null);
                        root.AddChild(label);
                    }

                    
                }
            }
        }

        public static void ClearSelected(GUI_Base root)
        {
            int i = 0;
            XNA_GUI.GUIElements.GUI_Base slider = null;
            string test = string.Format(c_sliderName, i);
            if (test == null)
                return;
            while ((slider = root.GetChildByName(string.Format(c_sliderName, i))) != null)
            {

                root.RemoveChild(slider);
                GUI_Base cancelButton = root.GetChildByName(string.Format("Cancel Button {0}", i));
                root.RemoveChild(cancelButton);
                GUI_Base name = root.GetChildByName(string.Format("Label {0}", i));
                root.RemoveChild(name);

                i++;
            }

            s_root = null;

            TextLabel planetName = (TextLabel)root.GetChildByName("Planet Name");
            TextLabel defenseLabel = (TextLabel)root.GetChildByName("Defense Fleets");
            TextLabel production = (TextLabel)root.GetChildByName("Production");
            planetName.DisplayText = "No Planet Selected";
            defenseLabel.DisplayText = "No Planet";
            production.DisplayText = "Production: No Planet";
            s_previousSelected = null;

        }

        /// <summary>
        /// Update the info for the selected. This updates production / defense information  in the GUI as well as 
        /// updates the planet's depolyment route distrobutions.  
        /// </summary>
        /// <param name="root">Root GUI elemtn that contains all the sliderbars for the deployment routes</param>
        /// <param name="viewing">The player viewing this information the screen.  The player's access to 
        /// to the planet is determined by the its access level</param>
        public static void Update(XNA_GUI.GUIElements.GUI_Base root, Player viewing)
        {
            if (s_previousSelected == null)
                return;
            TextLabel planetName = (TextLabel)root.GetChildByName("Planet Name");
            TextLabel defenseLabel = (TextLabel)root.GetChildByName("Defense Fleets");
            TextLabel production = (TextLabel)root.GetChildByName("Production");

            planetName.DisplayText = s_previousSelected.Name;
            defenseLabel.DisplayText = "Deffense Fleets: " + s_previousSelected.DefenseFleets.ToString();

            if (viewing.CanSeePlanet(s_previousSelected))
                production.DisplayText = string.Format("Production: {0} per min", (int)s_previousSelected.Production);
            else
                production.DisplayText = "No Data Available";

            //store the depolyment route distrobutions;
            SliderBar s = null;
            int index = 0;
            List<float> routes = s_previousSelected.DispatchRates;

            while ((s = (SliderBar)root.GetChildByName(string.Format(c_sliderName, index))) != null)
            {
                routes[index] = (float)s.CurrentValue / (float)s.MaxValue;
                index++;
            }

            s_previousSelected.NormalizeRoutes();

            index = 0;
            while ((s = (SliderBar)root.GetChildByName(string.Format(c_sliderName, index))) != null)
            {
                s.CurrentValue = (int)(s_previousSelected.DispatchRates[index] * s.MaxValue);
                index++;
            }
        }

        public static void RemoveSingeRoute(int dispatchIndex)
        {
            GUI_Base slider = s_root.GetChildByName(string.Format("Deployment Route {0}", dispatchIndex));
            s_root.RemoveChild(slider);
            slider = null;
            GUI_Base cancelButton = s_root.GetChildByName(string.Format("Cancel Button {0}", dispatchIndex));
            s_root.RemoveChild(cancelButton);
            cancelButton = null;
            GUI_Base name = s_root.GetChildByName(string.Format("Label {0}", dispatchIndex));
            s_root.RemoveChild(name);
            name = null;

            dispatchIndex++;
            SliderBar s = null;
            while ((s = (SliderBar)s_root.GetChildByName(string.Format("Deployment Route {0}", dispatchIndex))) != null)
            {
                s.ControlName = string.Format("Deployment Route {0}", dispatchIndex - 1);
                GUI_Base button = s_root.GetChildByName(string.Format("Cancel Button {0}", dispatchIndex));
                button.ControlName = string.Format("Cancel Button {0}", dispatchIndex - 1);
                GUI_Base label = s_root.GetChildByName(string.Format("Label {0}", dispatchIndex));
                label.ControlName = string.Format("Label {0}", dispatchIndex - 1);
                dispatchIndex++;
            }
        }
    }
}
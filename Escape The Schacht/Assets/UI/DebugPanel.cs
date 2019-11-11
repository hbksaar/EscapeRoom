using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeTheSchacht.UI {

    public class DebugPanel : MonoBehaviour {

        public Transform messageView;
        public Scrollbar scrollbar;
        public Button btnExpand, btnFile;
        //public Toggle chkAutoScroll;

        public Toggle chkLevelError, chkLevelWarning, chkLevelInfo, chkLevelVerbose, chkLevelDebug;
        private Text[] logLevelToggleLabels;
        private string[] logLevelToggleCaptions;
        private bool[] logLevelFlags;
        private int[] logLevelMsgCounts;

        private Text[] msgLabels;
        private int topMsgIndex;

        private RectTransform rTransform;
        private float collapsedBottomValue;
        public float expandedBottomValue = -80;
        private bool expanded = false;
        private bool handleScrollbarChange = true;

        public GameObject prefabTextLine;
        public int messageBufferSize = 100;
        public int collapsedLabelCount = 7; // number of lines to display in collapsed state
        public int expandedLabelCount = 30; // number of lines to display in expanded state
        public bool keepErrors = true;

        private readonly string msgFormat = "<color={0}><b>[{1:HH:mm:ss}] [{2}]</b> <i>{3}</i></color>";
        private List<MessageLoggedEventArgs> messages = new List<MessageLoggedEventArgs>();

        // Use this for initialization
        void Awake() {
            if (expandedLabelCount < collapsedLabelCount)
                throw new ArgumentException("messagesExpanded < messagesCollapsed");

            msgLabels = new Text[expandedLabelCount];
            for (int i = 0; i < msgLabels.Length; i++) {
                GameObject textLineGO = Instantiate(prefabTextLine, messageView) as GameObject;
                msgLabels[i] = textLineGO.GetComponent<Text>();
            }

            rTransform = (RectTransform) transform;
            collapsedBottomValue = rTransform.sizeDelta.y;

            logLevelFlags = new bool[Log.LevelDebug + 1];
            logLevelMsgCounts = new int[logLevelFlags.Length];
            logLevelToggleLabels = new Text[] {
                chkLevelError.GetComponentInChildren<Text>(),
                chkLevelWarning.GetComponentInChildren<Text>(),
                chkLevelInfo.GetComponentInChildren<Text>(),
                chkLevelVerbose.GetComponentInChildren<Text>(),
                chkLevelDebug.GetComponentInChildren<Text>()
            };
            logLevelToggleCaptions = new string[logLevelToggleLabels.Length];
            for (int i = 0; i < logLevelToggleCaptions.Length; i++) {
                logLevelToggleCaptions[i] = logLevelToggleLabels[i].text;
                logLevelToggleLabels[i].text += " (0)";
            }
            logLevelFlags[Log.LevelError] = chkLevelError.isOn;
            logLevelFlags[Log.LevelWarning] = chkLevelWarning.isOn;
            logLevelFlags[Log.LevelInfo] = chkLevelInfo.isOn;
            logLevelFlags[Log.LevelVerbose] = chkLevelVerbose.isOn;
            logLevelFlags[Log.LevelDebug] = chkLevelDebug.isOn;

            //foreach (Text label in msgLabels)
            //    label.gameObject.SetActive(false);

            Log.OnMessageLogged += OnMessageLogged;
        }

        private string GetPrefix(int logLevel) {
            switch (logLevel) {
            case Log.LevelError:
                return "E";
            case Log.LevelWarning:
                return "W";
            case Log.LevelInfo:
                return "I";
            case Log.LevelVerbose:
                return "V";
            case Log.LevelDebug:
                return "D";
            default:
                throw new ArgumentException("Invalid log level: " + logLevel);
            }
        }

        private string GetColor(int logLevel) {
            switch (logLevel) {
            case Log.LevelError:
                return "#FF0000FF";
            case Log.LevelWarning:
                return "#FF7700FF";
            case Log.LevelDebug:
                return "#555555FF";
            case Log.LevelVerbose:
                return "#777777FF";
            case Log.LevelInfo:
            default:
                return "#DDDDDDFF";
            }
        }

        private enum DebugPanelEvent {
            MessageReceived,
            ViewScrolled,
            LevelChanged,
            ViewExpanded,
            ViewReduced
        }

        private void UpdateMessageLabels(int firstMsgIndex, int firstLblIndex, int lblCount) {
            //Debug.LogFormat("DebugPanel.updateMessageLabels({0}, {1}, {2})", firstMsgIndex, firstLblIndex, lblCount);

            // TO.DO if we are at the bottom (value of topMsgIndex such that the last message is shown in a visible label: fill the labels beginning with the last to avoid gaps)

            //Debug.LogFormat("updateMessageLabels(firstMsgIndex={0}, firstLblIndex={1}, lblCount={2})", firstMsgIndex, firstLblIndex, lblCount);
            int lblIndex = firstLblIndex;
            int msgIndex = firstMsgIndex;
            //Debug.LogFormat("{0} < {1} + {2} ({5}) && {3} < {4} ({6})", lblIndex, firstLblIndex, lblCount, msgIndex, messages.Count, lblIndex < firstLblIndex + lblCount, msgIndex < messages.Count);
            while (lblIndex < firstLblIndex + lblCount && msgIndex < messages.Count) {
                MessageLoggedEventArgs msg = messages[msgIndex];
                if (logLevelFlags[msg.LogLevel]) {
                    //Debug.LogFormat("Updating lblIndex {0} (of {2}) with msgIndex {1} (of {3})", lblIndex, msgIndex, msgLabels.Length, messages.Count);
                    msgLabels[lblIndex].text = string.Format(msgFormat, GetColor(msg.LogLevel), msg.Timestamp, GetPrefix(msg.LogLevel), msg.Message);
                    msgLabels[lblIndex].gameObject.SetActive(true);
                    msgIndex++;
                    lblIndex++;
                }
                else
                    msgIndex++;
                //Debug.LogFormat("{0} < {1} + {2} ({5}) && {3} < {4} ({6})", lblIndex, firstLblIndex, lblCount, msgIndex, messages.Count, lblIndex < firstLblIndex + lblCount, msgIndex < messages.Count);
            }

            // if there are no more messages but there are more labels, make all subsequent labels invisible
            while (lblIndex < msgLabels.Length) {
                msgLabels[lblIndex].gameObject.SetActive(false);
                lblIndex++;
            }
        }

        private void UpdateMessageView(DebugPanelEvent e) {
            //print("DebugPanel.updateMessageView: " + e.ToString());

            switch (e) {
            case DebugPanelEvent.ViewExpanded:
                // TO.DO adjust top message index if there would be a gap at the bottom

                // show the labels beyond the collapsed view area and fill them with messages
                UpdateMessageLabels(topMsgIndex + collapsedLabelCount, collapsedLabelCount, expandedLabelCount - collapsedLabelCount);
                break;

            case DebugPanelEvent.ViewReduced:
                // TO.DO adjust top message index if we are at the bottom and auto-scrolling

                { // hide all labels that are not in the collapsed view area
                    for (int lblIndex = collapsedLabelCount; lblIndex < expandedLabelCount; lblIndex++)
                        msgLabels[lblIndex].gameObject.SetActive(false);
                }
                break;

            case DebugPanelEvent.ViewScrolled:
                if (!handleScrollbarChange)
                    break;
                { // recalculate the top message index and update all visible labels accordingly
                    int visibleLabelCount = expanded ? expandedLabelCount : collapsedLabelCount;
                    topMsgIndex = Mathf.Max(0, Mathf.RoundToInt(scrollbar.value * (messages.Count - visibleLabelCount)));
                    //print(topMsgIndex);
                    UpdateMessageLabels(topMsgIndex, 0, visibleLabelCount);
                }
                break;

            case DebugPanelEvent.LevelChanged: 
                { // update all visible labels accordingly
                    int visibleLabelCount = expanded ? expandedLabelCount : collapsedLabelCount;
                    UpdateMessageLabels(topMsgIndex, 0, visibleLabelCount);
                }
                break;

            case DebugPanelEvent.MessageReceived: 
                {
                    //print(scrollbar.value + " / " + topMsgIndex);
                    int visibleLabelCount = expanded ? expandedLabelCount : collapsedLabelCount;
                    if (visibleLabelCount >= messages.Count - topMsgIndex) {
                        // if there were or still are less messages than visible labels: update the first unused label
                        UpdateMessageLabels(messages.Count - 1, messages.Count - topMsgIndex - 1, 1);
                    } else if (scrollbar.value == 1f) {
                        // else if the scrollbar is at the bottom: apply auto-scrolling and update all labels to shift the old messages up and append the new one at the bottom
                        topMsgIndex++;
                        UpdateMessageLabels(topMsgIndex, 0, visibleLabelCount);
                        // FIX.ME da entsteht ne lücke, wenn messages dazu kommen, die nach aktuellem log level nicht angezeigt werden
                    } else {
                        // otherwise the new message is not displayed, but we need to update the scrollbar position
                        Debug.Assert(messages.Count > 0, "messages.Count == 0");
                        Debug.Assert(messages.Count - visibleLabelCount > 0, "messages.Count - visibleLabelCount <= 0");
                        handleScrollbarChange = false;
                        scrollbar.value = topMsgIndex / (float) (messages.Count - visibleLabelCount);
                        //Debug.LogFormat("DebugPanel.updateMessageView(MessageReceived, invisible): {0} / ({1} - {2}) = {3}", topMsgIndex, messages.Count, visibleLabelCount, topMsgIndex / (messages.Count - visibleLabelCount));
                        handleScrollbarChange = true;
                    }
                }
                break;

            }
        }

        private void OnMessageLogged(object sender, MessageLoggedEventArgs e) {
            //print("DebugPanel.OnMessageLogged");

            // remove oldest messages if list is full
            int surplusMessages = messages.Count + 1 - messageBufferSize;
            if (keepErrors)
                surplusMessages -= logLevelMsgCounts[Log.LevelError];

            if (surplusMessages > 0 && !keepErrors)
                messages.RemoveRange(0, surplusMessages);
            else if (surplusMessages > 0 && keepErrors)
                for (int i = 0; surplusMessages > 0 && i < messages.Count; /* nothing here */) {
                    if (messages[i].LogLevel == Log.LevelError)
                        i++; // keep error messages
                    else {
                        messages.RemoveAt(i);
                        surplusMessages--;
                    }
                }

            // update toggle labels
            logLevelMsgCounts[e.LogLevel]++;
            logLevelToggleLabels[e.LogLevel].text = string.Format("{0} ({1})", logLevelToggleCaptions[e.LogLevel], logLevelMsgCounts[e.LogLevel]);
            if (e.LogLevel == Log.LevelError || e.LogLevel == Log.LevelWarning)
                logLevelToggleLabels[e.LogLevel].fontStyle = FontStyle.Bold;

            // add message to list
            messages.Add(e);
            UpdateMessageView(DebugPanelEvent.MessageReceived);
        }

        private void OnNewLogfileStarted() {
            messages.Clear();
            topMsgIndex = 0;
            int visibleLabelCount = expanded ? expandedLabelCount : collapsedLabelCount;
            UpdateMessageLabels(0, 0, visibleLabelCount);
        }
        
        public void OnScrollbarValueChanged(Scrollbar sender) {
            if (sender != scrollbar)
                return;

            UpdateMessageView(DebugPanelEvent.ViewScrolled);
        }

        public void OnLogLevelChanged(Toggle sender) {
            if (sender == chkLevelError)
                logLevelFlags[Log.LevelError] = sender.isOn;
            else if (sender == chkLevelWarning)
                logLevelFlags[Log.LevelWarning] = sender.isOn;
            else if (sender == chkLevelInfo)
                logLevelFlags[Log.LevelInfo] = sender.isOn;
            else if (sender == chkLevelVerbose)
                logLevelFlags[Log.LevelVerbose] = sender.isOn;
            else if (sender == chkLevelDebug)
                logLevelFlags[Log.LevelDebug] = sender.isOn;
            else
                return;

            UpdateMessageView(DebugPanelEvent.LevelChanged);
        }
        
        public void OnButtonClick(Button sender) {
            if (sender == btnExpand) {

                bool doExpand = !expanded;
                expanded = !expanded;

                Rect rect = rTransform.rect;
                float height = doExpand ? expandedBottomValue : collapsedBottomValue;
                rTransform.sizeDelta = new Vector2(rTransform.sizeDelta.x, height);
                btnExpand.GetComponentInChildren<Text>().text = doExpand ? "Collapse" : "Expand";

                if (doExpand)
                    UpdateMessageView(DebugPanelEvent.ViewExpanded);
                else
                    UpdateMessageView(DebugPanelEvent.ViewReduced);
            }

            if (sender == btnFile) {
                System.Diagnostics.Process notepad = new System.Diagnostics.Process();
                notepad.StartInfo.FileName = "notepad";
                notepad.StartInfo.Arguments = Log.Instance.FilePath;
                notepad.Start();
            }
        }

    }

}

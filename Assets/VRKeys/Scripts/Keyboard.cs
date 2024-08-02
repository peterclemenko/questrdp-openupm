﻿/**
 * Copyright (c) 2017 The Campfire Union Inc - All Rights Reserved.
 *
 * Licensed under the MIT license. See LICENSE file in the project root for
 * full license information.
 *
 * Email:   info@campfireunion.com
 * Website: https://www.campfireunion.com
 */

using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using TMPro;

namespace VRKeys
{


    public class Keyboard : MonoBehaviour
    {


        public KeyboardLayout keyboardLayout = KeyboardLayout.Qwerty;

        [Space(15)]
        public TextMeshProUGUI placeholder;

        public string placeholderMessage = "Tap the keys to begin typing";

        public TextMeshProUGUI displayText;

        public TextMeshProUGUI validationMessage;

        public TextMeshProUGUI infoMessage;



        public TextMeshProUGUI successMessage;

        [Space(15)]
        public Color displayTextColor = Color.black;

        public Color caretColor = Color.gray;

        [Space(15)]
        public GameObject keyPrefab;

        public Transform keysParent;

        public float keyWidth = 0.16f;

        public float keyHeight = 0.16f;

        [Space(15)]
        public string text = "";

        [Space(15)]
        public GameObject canvas;

        public GameObject leftMallet;

        public GameObject rightMallet;

        public GameObject keyboardWrapper;

        public ShiftKey shiftKey;

        public Key[] extraKeys;

        [Space(15)]
        public bool leftPressing = false;

        public bool rightPressing = false;

        public bool initialized = false;

        public bool disabled = true;

        [Serializable]
        public class KeyboardUpdateEvent : UnityEvent<string> { }

        [Serializable]
        public class KeyboardSubmitEvent : UnityEvent<string> { }




        /// <summary>
        /// Listen for events whenever the text changes.
        /// </summary>
        public KeyboardUpdateEvent OnUpdate = new KeyboardUpdateEvent();

        /// <summary>
        /// Listen for events when Submit() is called.
        /// </summary>
        public KeyboardSubmitEvent OnSubmit = new KeyboardSubmitEvent();

        /// <summary>
        /// Listen for events when Cancel() is called.
        /// </summary>
        public UnityEvent OnCancel = new UnityEvent();

        public class KeyboardAddCharacterEvent : UnityEvent<string> { }
        public KeyboardAddCharacterEvent OnAddChar = new KeyboardAddCharacterEvent();

        public class KeyboardBackspaceEvent : UnityEvent { }
        public KeyboardBackspaceEvent OnBackspace = new KeyboardBackspaceEvent();

        public Transform player;

        public GameObject leftHand;

        public GameObject rightHand;

        private LetterKey[] keys;

        private bool shifted = false;

        private Layout layout;

        /// <summary>
        /// Initialization.
        /// </summary>
        private IEnumerator Start()
        {
            yield return StartCoroutine(DoSetLanguage(keyboardLayout));

            UpdateDisplayText();
            PlaceholderVisibility();

            initialized = true;
        }

        private void PositionAndAttachMallets()
        {
            var offset = new Vector3(0f, 0f, 1.8f);
            var position = player.transform.position +
               player.transform.up * offset.y +
               player.transform.right * offset.x +
               player.transform.forward * offset.z;
            transform.position = position;
            transform.rotation = player.transform.rotation;

            leftMallet.transform.SetParent(leftHand.transform);
            leftMallet.transform.localPosition = Vector3.zero;
            leftMallet.transform.localPosition = Vector3.down * 0.1f;
            leftMallet.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            leftMallet.SetActive(true);

            rightMallet.transform.SetParent(rightHand.transform);
            rightMallet.transform.localPosition = Vector3.zero;
            rightMallet.transform.localPosition = Vector3.down * 0.1f;
            rightMallet.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rightMallet.SetActive(true);

            keysParent.localEulerAngles = new Vector3(-30, 0, 0);
            keysParent.localPosition = new Vector3(0f, -0.374431f, -0.7368833f);
        }

        private void DetachMallets()
        {
            if (leftMallet != null)
            {
                leftMallet.SetActive(false);
            }

            if (rightMallet != null)
            {
                rightMallet.SetActive(false);
            }
        }

        private void OnDisable()
        {
            Disable();
        }

        public void Enable()
        {
            if (!initialized)
            {
                // Make sure we're initialized first.
                StartCoroutine(EnableWhenInitialized());
                return;
            }

            disabled = false;

            if (canvas != null)
            {
                canvas.SetActive(true);
            }

            if (keysParent != null)
            {
                keysParent.gameObject.SetActive(true);
            }

            EnableInput();

            PositionAndAttachMallets();
        }

        private IEnumerator EnableWhenInitialized()
        {
            yield return new WaitUntil(() => initialized);

            Enable();
        }

        public void Disable()
        {
            disabled = true;

            if (canvas != null)
            {
                canvas.SetActive(false);
            }

            if (keysParent != null)
            {
                keysParent.gameObject.SetActive(false);
            }

            DetachMallets();
        }

        /// <summary>
        /// Set the text value all at once.
        /// </summary>
        /// <param name="txt">New text value.</param>
        public void SetText(string txt)
        {
            text = txt;

            UpdateDisplayText();
            PlaceholderVisibility();

            OnUpdate.Invoke(text);
        }

        /// <summary>
        /// Add a character to the input text.
        /// </summary>
        /// <param name="character">Character.</param>
        public void AddCharacter(string character)
        {
            text += character;

            UpdateDisplayText();
            PlaceholderVisibility();

            OnUpdate.Invoke(text);
            OnAddChar.Invoke(character);

            if (shifted && character != "" && character != " ")
            {
                StartCoroutine(DelayToggleShift());
            }
        }

        /// <summary>
        /// Toggle whether the characters are shifted (caps).
        /// </summary>
        public bool ToggleShift()
        {
            if (keys == null)
            {
                return false;
            }

            shifted = !shifted;

            foreach (LetterKey key in keys)
            {
                key.shifted = shifted;
            }

            shiftKey.Toggle(shifted);

            return shifted;
        }

        private IEnumerator DelayToggleShift()
        {
            yield return new WaitForSeconds(0.1f);

            ToggleShift();
        }

        /// <summary>
        /// Disable keyboard input.
        /// </summary>
        public void DisableInput()
        {
            leftPressing = false;
            rightPressing = false;

            if (keys != null)
            {
                foreach (LetterKey key in keys)
                {
                    if (key != null)
                    {
                        key.Disable();
                    }
                }
            }

            foreach (Key key in extraKeys)
            {
                key.Disable();
            }
        }

        /// <summary>
        /// Re-enable keyboard input.
        /// </summary>
        public void EnableInput()
        {
            leftPressing = false;
            rightPressing = false;

            PositionAndAttachMallets();

            if (keys != null)
            {
                foreach (LetterKey key in keys)
                {
                    if (key != null)
                    {
                        key.Enable();
                    }
                }
            }

            foreach (Key key in extraKeys)
            {
                key.Enable();
            }
        }

        /// <summary>
        /// Backspace one character.
        /// </summary>
        public void Backspace()
        {
            if (text.Length > 0)
            {
                text = text.Substring(0, text.Length - 1);
            }

            UpdateDisplayText();
            PlaceholderVisibility();

            OnUpdate.Invoke(text);
            OnBackspace.Invoke();
        }

        /// <summary>
        /// Submit and close the keyboard.
        /// </summary>
        public void Submit()
        {
            OnSubmit.Invoke(text);
        }

        /// <summary>
        /// Cancel input and close the keyboard.
        /// </summary>
        public void Cancel()
        {
            OnCancel.Invoke();
            Disable();
        }

        /// <summary>
        /// Set the language of the keyboard.
        /// </summary>
        /// <param name="layout">New language.</param>
        public void SetLayout(KeyboardLayout layout)
        {
            StartCoroutine(DoSetLanguage(layout));
        }

        private IEnumerator DoSetLanguage(KeyboardLayout lang)
        {
            keyboardLayout = lang;
            layout = LayoutList.GetLayout(keyboardLayout);

            placeholderMessage = layout.placeholderMessage;

            yield return StartCoroutine(SetupKeys());

            // Update extra keys
            foreach (Key key in extraKeys)
            {
                key.UpdateLayout(layout);
            }
        }

        /// <summary>
        /// Set a custom placeholder message.
        /// </summary>
        /// <param name="msg">Message.</param>
        public void SetPlaceholderMessage(string msg)
        {
            StartCoroutine(DoSetPlaceholderMessage(msg));
        }

        private IEnumerator DoSetPlaceholderMessage(string msg)
        {
            if (!initialized)
            {
                yield return new WaitUntil(() => initialized);
            }

            placeholder.text = placeholderMessage = msg;

            yield break;
        }

        /// <summary>
        /// Show the specified validation notice.
        /// </summary>
        /// <param name="message">Message to show.</param>
        public void ShowValidationMessage(string message)
        {
            validationMessage.text = message;

        }

        /// <summary>
        /// Show the specified input notice.
        /// </summary>
        /// <param name="message">Message to show.</param>
        public void ShowInfoMessage(string message)
        {
            infoMessage.text = message;

        }

        /// <summary>
        /// Show the specified success notice.
        /// </summary>
        /// <param name="message">Message to show.</param>
        public void ShowSuccessMessage(string message)
        {
            successMessage.text = message;

        }





        /// <summary>
        /// Setup the keys.
        /// </summary>
        private IEnumerator SetupKeys()
        {
            bool activeState = canvas.activeSelf;

            // Hide everything before setting up the keys
            canvas.SetActive(false);
            keysParent.gameObject.SetActive(false);

            // Remove previous keys
            if (keys != null)
            {
                foreach (Key key in keys)
                {
                    if (key != null)
                    {
                        Destroy(key.gameObject);
                    }
                }
            }

            keys = new LetterKey[layout.TotalKeys()];
            int keyCount = 0;

            // Numbers row
            for (int i = 0; i < layout.row1Keys.Length; i++)
            {
                GameObject obj = (GameObject)Instantiate(keyPrefab, keysParent);
                obj.transform.localPosition += (Vector3.right * ((keyWidth * i) - layout.row1Offset));

                LetterKey key = obj.GetComponent<LetterKey>();
                key.character = layout.row1Keys[i];
                key.shiftedChar = layout.row1Shift[i];
                key.shifted = false;
                key.Init(obj.transform.localPosition);

                obj.name = "Key: " + layout.row1Keys[i];
                obj.SetActive(true);

                keys[keyCount] = key;
                keyCount++;

                yield return null;
            }

            // QWERTY row
            for (int i = 0; i < layout.row2Keys.Length; i++)
            {
                GameObject obj = (GameObject)Instantiate(keyPrefab, keysParent);
                obj.transform.localPosition += (Vector3.right * ((keyWidth * i) - layout.row2Offset));
                obj.transform.localPosition += (Vector3.back * keyHeight * 1);

                LetterKey key = obj.GetComponent<LetterKey>();
                key.character = layout.row2Keys[i];
                key.shiftedChar = layout.row2Shift[i];
                key.shifted = false;
                key.Init(obj.transform.localPosition);

                obj.name = "Key: " + layout.row2Keys[i];
                obj.SetActive(true);

                keys[keyCount] = key;
                keyCount++;

                yield return null;
            }

            // ASDF row
            for (int i = 0; i < layout.row3Keys.Length; i++)
            {
                GameObject obj = (GameObject)Instantiate(keyPrefab, keysParent);
                obj.transform.localPosition += (Vector3.right * ((keyWidth * i) - layout.row3Offset));
                obj.transform.localPosition += (Vector3.back * keyHeight * 2);

                LetterKey key = obj.GetComponent<LetterKey>();
                key.character = layout.row3Keys[i];
                key.shiftedChar = layout.row3Shift[i];
                key.shifted = false;
                key.Init(obj.transform.localPosition);

                obj.name = "Key: " + layout.row3Keys[i];
                obj.SetActive(true);

                keys[keyCount] = key;
                keyCount++;

                yield return null;
            }

            // ZXCV row
            for (int i = 0; i < layout.row4Keys.Length; i++)
            {
                GameObject obj = (GameObject)Instantiate(keyPrefab, keysParent);
                obj.transform.localPosition += (Vector3.right * ((keyWidth * i) - layout.row4Offset));
                obj.transform.localPosition += (Vector3.back * keyHeight * 3);

                LetterKey key = obj.GetComponent<LetterKey>();
                key.character = layout.row4Keys[i];
                key.shiftedChar = layout.row4Shift[i];
                key.shifted = false;
                key.Init(obj.transform.localPosition);

                obj.name = "Key: " + layout.row4Keys[i];
                obj.SetActive(true);

                keys[keyCount] = key;
                keyCount++;

                yield return null;
            }

            // Reset visibility of canvas and keyboard
            canvas.SetActive(activeState);
            keysParent.gameObject.SetActive(activeState);
        }

        /// <summary>
        /// Update the display text, including trailing caret.
        /// </summary>
        private void UpdateDisplayText()
        {
            string display = (text.Length > 37) ? text.Substring(text.Length - 37) : text;

            displayText.text = string.Format(
                "<#{0}>{1}</color><#{2}>_</color>",
                ColorUtility.ToHtmlStringRGB(displayTextColor),
                display,
                ColorUtility.ToHtmlStringRGB(caretColor)
            );
        }

        /// <summary>
        /// Show/hide placeholder text.
        /// </summary>
        private void PlaceholderVisibility()
        {
            if (text == "")
            {
                placeholder.text = placeholderMessage;
                placeholder.gameObject.SetActive(true);
            }
            else
            {
                placeholder.gameObject.SetActive(false);
            }
        }
    }
}
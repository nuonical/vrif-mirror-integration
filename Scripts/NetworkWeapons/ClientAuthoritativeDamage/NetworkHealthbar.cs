using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace BNG
{
    // while not a networked item, it is an example of a simple health bar that is updated from the NetworkDamagable script
    public class NetworkHealthbar : MonoBehaviour
    {
        public Image healthBar;
        private float lerpSpeed = 3f;
        public TMP_Text currentHealthText;

        public NetworkDamageable nD;

        private void Start()
        {
            if (!nD)
            {
                nD = GetComponent<NetworkDamageable>();
            }
        }

        private void Update()
        {
            if (nD)
            {
                HeathBarController(nD._currentHealth, nD.maxHealth);

            }
        }

        public void HeathBarController(float currentHealth, float maxHealth)
        {
            // update the fill amount of the healthbar
            healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, currentHealth / maxHealth, lerpSpeed * Time.deltaTime);

            // update the text ui element with current health amount
            if (currentHealthText != null)
            {
                currentHealthText.text = currentHealth.ToString("F0") + "/" + maxHealth.ToString("F0");
            }

            // change the color from green to red as health goes down or up
            Color healthColor = Color.Lerp(Color.red, Color.green, currentHealth / maxHealth);

            if (healthBar != null)
            {
                healthBar.color = healthColor;
            }
        }
    }
}

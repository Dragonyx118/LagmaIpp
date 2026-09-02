# LagmaIpp

### *App companion per L.A.G.M.A. B.I.L.L.S.*

![Framework](https://img.shields.io/badge/framework-.NET%20MAUI-512BD4?style=flat&logo=dotnet&logoColor=white)
![Language](https://img.shields.io/badge/language-C%23-239120?style=flat&logo=csharp&logoColor=white)
![Platform](https://img.shields.io/badge/platform-Windows%20%2F%20Android-0078D4?style=flat&logo=windows&logoColor=white)
![MQTT](https://img.shields.io/badge/protocol-MQTT-660066?style=flat&logo=mqtt&logoColor=white)
![Repo principale](https://img.shields.io/badge/robot-LagmaBills-gray?style=flat&logo=github&logoColor=white)

App companion per il robot [LagmaBills](https://github.com/Dragonyx118/LagmaBills), sviluppata in **.NET MAUI** per Windows e Android. Questa applicazione permette di controllare, monitorare e interagire in tempo reale con il sistema robotico di protezione civile tramite comunicazione MQTT.

---

## 🚀 Funzionalità Principali

*   **Telecomando di Movimento:** Interfaccia dedicata per la gestione della cinematica a ruote Mecanum (movimenti omnidirezionali, rotazione e traslazione).
*   **Telemetria in Tempo Reale:** Visualizzazione dello stato dei motori, dei sensori di distanza (HC-SR04), della IMU e della posizione odometrica.
*   **Streaming Video:** Visualizzazione in diretta del flusso video proveniente dalla telecamera integrata sul Raspberry Pi.
*   **Gestione Navigazione:** Invio di coordinate di obiettivo (*goal frames*) per guidare la navigazione autonoma del robot.
*   **Controllo Periferiche:** Comandi rapidi per l'azionamento del braccio robotico e la supervisione del drone companion (*ESP-Drone*).

---

## 📡 Architettura di Comunicazione

L'app si connette direttamente al broker MQTT (`Mosquitto`) eseguito sul Raspberry Pi centrale (accessibile tramite rete locale Wi-Fi o VPN protetta come Tailscale). I principali topic utilizzati comprendono:

*   `robot/motori/cmd`: Invio comandi di velocità e direzione.
*   `robot/motori/stato`: Ricezione dati da encoder e PWM.
*   `robot/sensori/distanze`: Lettura delle distanze dagli ostacoli.
*   `robot/nav/goal`: Invio delle coordinate di navigazione autonoma.

---

## 📦 Download e Rilasci

È possibile scaricare l'APK precompilato dell'applicazione Android direttamente dalla sezione **[Releases](https://github.com/Dragonyx118/LagmaIpp/releases)** del repository. Le release vengono aggiornate automaticamente ad ogni nuova versione del codice tramite GitHub Actions.

---

## 👨‍💻 Crediti e Sviluppo

*   **Daniele Cerioli (Dragonyx)** — Sviluppo app, firmware e integrazione di sistema.
*   Progetto collegato al repository principale **[LagmaBills](https://github.com/Dragonyx118/LagmaBills)**.

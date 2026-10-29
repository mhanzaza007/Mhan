# Mhan

Mhan shares the song currently playing in the Apple Music app for Windows as your Discord Rich Presence. It displays the activity name **Apple Music**, plus the track title and artist. It also looks up album artwork in Apple's iTunes catalog when a match is available.

## Download

Download the latest Windows app from [GitHub Releases](https://github.com/mhanzaza007/Mhan/releases/latest). Download `Mhan.exe` from the latest release and run it. The app is self-contained; you do not need to install .NET.

## Requirements

- Windows 10 version 1809 or newer, or Windows 11
- Apple Music for Windows and Discord Desktop
- An internet connection for Discord IPC and album-art lookup

## First-time setup

1. Open the [Discord Developer Portal](https://discord.com/developers/applications) and select **New Application**.
2. Name the application `Mhan` and create it. You do not need to add a bot.
3. Open **General Information** and copy the **Application ID**.
4. In Discord, open **User Settings → Activity Privacy** and enable activity sharing.
5. Open Apple Music and Discord Desktop. Start `Mhan.exe`, paste the Application ID, then select **Save and Start Sharing**.
6. Play a song in Apple Music. Your Discord profile should show **Apple Music**, the song title, and the artist.

The Application ID is an identifier, not a password. Mhan does not ask for your Discord password, a bot token, or your Apple ID.

## Tray icon and Windows startup

After sharing starts, Mhan saves your Application ID and enables startup with Windows. Closing the window with **X** hides Mhan in the notification area beside the clock; it keeps running in the background. Double-click the Mhan tray icon to reopen the window. Right-click it to stop sharing, change the **Open when Windows starts** setting, or exit Mhan completely.

## Troubleshooting

- **Discord does not show Apple Music:** Keep Discord Desktop open, enable activity sharing in Activity Privacy, and make sure Mhan says it is sharing.
- **No song appears:** Start playback in the Apple Music Windows app on this PC. Paused or stopped playback is cleared from Discord.
- **No album artwork:** Artwork is looked up by matching the song against Apple's iTunes catalog. Some tracks may not have a match; the activity can still show the title and artist without artwork.
- **Mhan cannot connect:** Make sure Discord Desktop is running. The browser version of Discord is not enough for local Rich Presence.

## Automatic updates

Mhan checks the latest GitHub Release when it starts and every six hours while it is running. If a newer `Mhan.exe` is available, Mhan verifies its SHA-256 digest, replaces the executable in the same folder, and restarts. Windows startup continues to point to the same file. Only a published Release updates the app; pushing source code alone does not.

Because the repository is private, automatic updates need a GitHub fine-grained personal access token. In Mhan, select **Set up automatic updates**, then create a token with **Only select repositories → Mhan** and **Repository permissions → Contents: Read-only**. Paste it into Mhan to verify and save it. Mhan encrypts the token for your Windows user with DPAPI and uses it only to read the private Release. You can remove it from the same settings window. If you do not configure a token, music sharing still works, but automatic updates stay off.

## Build from source

Install the .NET 10 SDK, then run in PowerShell:

```powershell
dotnet build .\Mhan.csproj -c Release
```

## Privacy

While sharing is active, Mhan reads the current track title, artist, and playback state from the Windows media session and sends the track and artist to Discord as Rich Presence. It queries Apple's iTunes catalog to find artwork. If automatic updates are enabled, Mhan stores the GitHub read-only token encrypted for the current Windows account and sends it only to GitHub's API to check and download Releases. Mhan does not collect or transmit account passwords, Discord credentials, or Apple ID credentials.

---

# Mhan (ภาษาไทย)

Mhan แสดงเพลงที่กำลังเล่นใน Apple Music สำหรับ Windows บน Discord Rich Presence โดยขึ้นชื่อกิจกรรมเป็น **Apple Music** พร้อมชื่อเพลงและศิลปิน และค้นหารูปปกจากแคตตาล็อก iTunes ของ Apple เมื่อพบเพลงที่ตรงกัน

## ดาวน์โหลด

ดาวน์โหลดโปรแกรม Windows รุ่นล่าสุดจาก [GitHub Releases](https://github.com/mhanzaza007/Mhan/releases/latest) แล้วดาวน์โหลดไฟล์ `Mhan.exe` จากรุ่นล่าสุดและเปิดใช้งานได้เลย ตัวโปรแกรมรวม .NET Runtime มาแล้ว ไม่ต้องติดตั้ง .NET

## สิ่งที่ต้องมี

- Windows 10 version 1809 ขึ้นไป หรือ Windows 11
- แอป Apple Music สำหรับ Windows และ Discord Desktop
- อินเทอร์เน็ตสำหรับเชื่อมต่อ Discord และค้นหารูปปกเพลง

## ตั้งค่าครั้งแรก

1. เปิด [Discord Developer Portal](https://discord.com/developers/applications) แล้วเลือก **New Application**
2. ตั้งชื่อแอปว่า `Mhan` แล้วสร้างแอป ไม่ต้องเพิ่มบอท
3. เปิด **General Information** แล้วคัดลอก **Application ID**
4. ใน Discord ไปที่ **การตั้งค่าผู้ใช้ → ความเป็นส่วนตัวของกิจกรรม (Activity Privacy)** แล้วเปิดการแชร์กิจกรรม
5. เปิด Apple Music และ Discord Desktop จากนั้นเปิด `Mhan.exe` วาง Application ID แล้วกด **บันทึกและเริ่มแชร์**
6. เปิดเพลงใน Apple Music โปรไฟล์ Discord จะแสดง **Apple Music** พร้อมชื่อเพลงและศิลปิน

Application ID เป็นเพียงหมายเลขระบุแอป ไม่ใช่รหัสผ่าน Mhan จะไม่ขอรหัสผ่าน Discord, Bot Token หรือ Apple ID

## ไอคอนถาดระบบและการเปิดพร้อม Windows

เมื่อเริ่มแชร์แล้ว Mhan จะบันทึก Application ID และตั้งให้เปิดพร้อม Windows หากกด **X** หน้าต่างจะซ่อนลงไปทำงานเบื้องหลังใกล้นาฬิกา ดับเบิลคลิกไอคอน Mhan เพื่อเปิดหน้าต่างอีกครั้ง คลิกขวาที่ไอคอนเพื่อหยุดแชร์ เปลี่ยนการตั้งค่า **เปิดเมื่อเข้า Windows** หรือออกจากโปรแกรม

## แก้ปัญหาเบื้องต้น

- **Discord ไม่แสดง Apple Music:** เปิด Discord Desktop เปิดการแชร์กิจกรรมใน Activity Privacy และตรวจสอบว่า Mhan แจ้งว่ากำลังแชร์
- **ไม่พบเพลง:** เปิดเพลงในแอป Apple Music บนคอมเครื่องนี้ เพลงที่หยุดหรือพักไว้จะไม่แสดงบน Discord
- **ไม่มีรูปปก:** โปรแกรมค้นหารูปจากแคตตาล็อก iTunes ของ Apple บางเพลงอาจค้นไม่พบ แต่ยังแสดงชื่อเพลงและศิลปินได้
- **Mhan เชื่อมต่อไม่ได้:** เปิด Discord Desktop ค้างไว้ Discord เวอร์ชันเว็บอย่างเดียวไม่รองรับการเชื่อมต่อ Rich Presence ในเครื่อง

## การอัปเดตอัตโนมัติ

Mhan ตรวจสอบ GitHub Release ล่าสุดตอนเปิดโปรแกรมและทุก 6 ชั่วโมงระหว่างทำงาน หากมี `Mhan.exe` รุ่นใหม่ โปรแกรมจะตรวจสอบ SHA-256 แทนไฟล์เดิมในโฟลเดอร์เดิม แล้วเปิดใหม่ การเปิดพร้อม Windows ยังชี้ไปยังไฟล์เดิม การอัปเดตจะเกิดเมื่อมีการเผยแพร่ Release แล้วเท่านั้น การอัปโหลดซอร์สโค้ดอย่างเดียวยังไม่อัปเดตโปรแกรม

เนื่องจาก repository เป็น private จึงต้องใช้ GitHub fine-grained personal access token ใน Mhan คลิก **ตั้งค่าอัปเดตอัตโนมัติ** แล้วสร้าง token โดยเลือก **Only select repositories → Mhan** และ **Repository permissions → Contents: Read-only** นำ token มาวางใน Mhan เพื่อให้ตรวจสอบและบันทึก โปรแกรมเข้ารหัส token ด้วย DPAPI สำหรับบัญชี Windows ปัจจุบัน และใช้ token เพื่ออ่าน Release ส่วนตัวเท่านั้น ลบ token ได้จากหน้าตั้งค่าเดียวกัน หากไม่ตั้งค่า token การแชร์เพลงยังทำงานได้ แต่จะไม่ตรวจอัปเดตอัตโนมัติ

## Build จากซอร์สโค้ด

ติดตั้ง .NET 10 SDK แล้วเปิด PowerShell ในโฟลเดอร์โปรเจกต์ จากนั้นรัน:

```powershell
dotnet build .\Mhan.csproj -c Release
```

## ความเป็นส่วนตัว

ระหว่างเปิดแชร์ Mhan จะอ่านชื่อเพลง ศิลปิน และสถานะการเล่นจาก Windows media session แล้วส่งชื่อเพลงและศิลปินไปยัง Discord เพื่อแสดง Rich Presence นอกจากนี้ยังค้นหารูปปกจากแคตตาล็อก iTunes ของ Apple หากเปิดใช้การอัปเดตอัตโนมัติ Mhan จะเก็บ GitHub read-only token แบบเข้ารหัสสำหรับบัญชี Windows ปัจจุบัน และส่ง token ไปยัง GitHub API เพื่อเช็กและดาวน์โหลด Release เท่านั้น โปรแกรมไม่เก็บหรือส่งรหัสผ่านบัญชี Discord หรือ Apple ID


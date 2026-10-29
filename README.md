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

## Updates

Mhan does not currently update itself. To update, exit Mhan from its tray icon, download the newest `Mhan.exe` from [Releases](https://github.com/mhanzaza007/Mhan/releases/latest), and run it. Changes pushed to the GitHub source do not update copies already on users' PCs; a new release must be published for users to download.

## Build from source

Install the .NET 10 SDK, then run in PowerShell:

```powershell
dotnet build .\Mhan.csproj -c Release
```

## Privacy

While sharing is active, Mhan reads the current track title, artist, and playback state from the Windows media session and sends the track and artist to Discord as Rich Presence. It queries Apple's iTunes catalog to find artwork. Mhan does not collect or transmit account passwords or tokens.

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

## การอัปเดต

ขณะนี้ Mhan ยังอัปเดตตัวเองไม่ได้ หากต้องการอัปเดต ให้คลิกขวาไอคอน Mhan แล้วออกจากโปรแกรม จากนั้นดาวน์โหลด `Mhan.exe` รุ่นล่าสุดจากหน้า [Releases](https://github.com/mhanzaza007/Mhan/releases/latest) แล้วเปิดไฟล์ใหม่ การอัปเดตซอร์สโค้ดบน GitHub จะไม่เปลี่ยนโปรแกรมที่ติดตั้งอยู่ในเครื่องผู้ใช้ ต้องเผยแพร่ Release รุ่นใหม่ให้ดาวน์โหลด

## Build จากซอร์สโค้ด

ติดตั้ง .NET 10 SDK แล้วเปิด PowerShell ในโฟลเดอร์โปรเจกต์ จากนั้นรัน:

```powershell
dotnet build .\Mhan.csproj -c Release
```

## ความเป็นส่วนตัว

ระหว่างเปิดแชร์ Mhan จะอ่านชื่อเพลง ศิลปิน และสถานะการเล่นจาก Windows media session แล้วส่งชื่อเพลงและศิลปินไปยัง Discord เพื่อแสดง Rich Presence นอกจากนี้ยังค้นหารูปปกจากแคตตาล็อก iTunes ของ Apple โปรแกรมไม่เก็บหรือส่งรหัสผ่านหรือโทเคนบัญชี


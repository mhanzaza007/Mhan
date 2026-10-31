# Mhan

Mhan shares the song currently playing in Apple Music or Spotify for Windows as your Discord Rich Presence. Choose the music services and Lyrics Preview separately in the app. When synced lyrics are available, Discord shows the current lyric line alongside the track and artist. Album artwork is looked up in Apple's iTunes catalog when a match is available.

## Download

Download the latest Windows app from [GitHub Releases](https://github.com/mhanzaza007/Mhan/releases/latest). Download `Mhan.exe` from the latest release and run it. The app is self-contained; you do not need to install .NET.

## Requirements

- Windows 10 version 1809 or newer, or Windows 11
- Apple Music for Windows and/or Spotify for Windows, plus Discord Desktop
- An internet connection for Discord IPC, album-art lookup, and synced lyrics

## First-time setup

1. Open the [Discord Developer Portal](https://discord.com/developers/applications) and select **New Application**.
2. Name the application `Mhan` and create it. You do not need to add a bot.
3. Open **General Information** and copy the **Application ID**.
4. In Discord, open **User Settings → Activity Privacy** and enable activity sharing.
5. Open Apple Music and/or Spotify and Discord Desktop. Start `Mhan.exe`, select the services and whether to share lyrics, paste the Application ID, then select **Save and Start Sharing**.
6. Play a song in a selected service. Your Discord profile shows the track and artist, plus the current lyric line when synced lyrics are available.

The Application ID is an identifier, not a password. Mhan does not ask for your Discord password, a bot token, or your Apple ID.

## Tray icon and Windows startup

After sharing starts, Mhan saves your Application ID and enables startup with Windows. Closing the window with **X** hides Mhan in the notification area beside the clock; it keeps running in the background. Double-click the Mhan tray icon to reopen the window. Right-click it to stop sharing, change the **Open when Windows starts** setting, or exit Mhan completely.

Use the **Apple Music** and **Spotify** checkboxes to choose which services can appear on Discord. The separate **Lyrics Preview** checkbox controls whether Mhan shares the current synced lyric line; turning it off does not stop music sharing. If both services are playing, Mhan prefers the playing session Windows reports first.

## Troubleshooting

- **Discord does not show the music activity:** Keep Discord Desktop open, enable activity sharing in Activity Privacy, and make sure Mhan says it is sharing.
- **No song appears:** Start playback in Apple Music or Spotify on this PC and check that service in Mhan. Paused or stopped playback is cleared from Discord.
- **No lyrics appear:** Make sure **Lyrics Preview** is checked. The track needs synced lyrics in LRCLIB or LrcMux; tracks with only plain lyrics do not have a reliable current line to show. Mhan retries after temporary service errors.
- **No album artwork:** Artwork is looked up by matching the song against Apple's iTunes catalog. Some tracks may not have a match; the activity can still show the title and artist without artwork.
- **Mhan cannot connect:** Make sure Discord Desktop is running. The browser version of Discord is not enough for local Rich Presence.

## Automatic updates

Mhan checks the latest public GitHub Release before starting music sharing and every six hours while it is running. If a newer `Mhan.exe` is available, Mhan verifies its SHA-256 digest, installs it into a folder named for the new release, updates the Windows startup path, and restarts. After the new copy starts successfully, Mhan removes the old executable and its empty version folder. If installation fails, it restores and reopens the previous copy. Mhan will not replace an installed version with an older release. Only a published Release updates the app; pushing source code alone does not.

Automatic updates work without a GitHub token because the Mhan repository is public. The **Automatic updates** settings can check access and optionally store a fine-grained read-only token if the repository becomes private. Any token is encrypted for your Windows user with DPAPI.

## Build from source

Install the .NET 10 SDK, then run in PowerShell:

```powershell
dotnet build .\Mhan.csproj -c Release
```

## Privacy

While sharing is active, Mhan reads the current track title, artist, playback state, and position from the Windows media session. It sends activity to Discord, queries Apple's iTunes catalog for artwork, and queries LRCLIB for synced lyrics, using LrcMux as a fallback. These lyric services receive the track title, artist, and album metadata. If automatic updates are enabled, Mhan stores the GitHub read-only token encrypted for the current Windows account and sends it only to GitHub's API. Mhan does not collect or transmit account passwords, Discord credentials, or Apple ID credentials.

---

# Mhan (ภาษาไทย)

Mhan แสดงเพลงที่กำลังเล่นใน Apple Music หรือ Spotify สำหรับ Windows บน Discord Rich Presence และเลือกแชร์เนื้อเพลงได้แยกต่างหาก เมื่อมี synced lyrics จะแสดงบรรทัดปัจจุบันตามตำแหน่งเพลง พร้อมชื่อบริการ เพลง และศิลปิน ส่วนรูปปกค้นจากแคตตาล็อก iTunes ของ Apple เมื่อพบเพลงที่ตรงกัน

## ดาวน์โหลด

ดาวน์โหลดโปรแกรม Windows รุ่นล่าสุดจาก [GitHub Releases](https://github.com/mhanzaza007/Mhan/releases/latest) แล้วดาวน์โหลดไฟล์ `Mhan.exe` จากรุ่นล่าสุดและเปิดใช้งานได้เลย ตัวโปรแกรมรวม .NET Runtime มาแล้ว ไม่ต้องติดตั้ง .NET

## สิ่งที่ต้องมี

- Windows 10 version 1809 ขึ้นไป หรือ Windows 11
- แอป Apple Music และ/หรือ Spotify สำหรับ Windows พร้อม Discord Desktop
- อินเทอร์เน็ตสำหรับเชื่อมต่อ Discord ค้นหารูปปก และค้นหา synced lyrics

## ตั้งค่าครั้งแรก

1. เปิด [Discord Developer Portal](https://discord.com/developers/applications) แล้วเลือก **New Application**
2. ตั้งชื่อแอปว่า `Mhan` แล้วสร้างแอป ไม่ต้องเพิ่มบอท
3. เปิด **General Information** แล้วคัดลอก **Application ID**
4. ใน Discord ไปที่ **การตั้งค่าผู้ใช้ → ความเป็นส่วนตัวของกิจกรรม (Activity Privacy)** แล้วเปิดการแชร์กิจกรรม
5. เปิด Apple Music และ/หรือ Spotify กับ Discord Desktop จากนั้นเปิด `Mhan.exe` ติ๊กบริการและตัวเลือกแชร์เนื้อเพลงตามต้องการ วาง Application ID แล้วกด **บันทึกและเริ่มแชร์**
6. เปิดเพลงในบริการที่เลือก โปรไฟล์ Discord จะแสดงเพลงและศิลปิน พร้อมบรรทัดเนื้อเพลงปัจจุบันเมื่อมี synced lyrics

Application ID เป็นเพียงหมายเลขระบุแอป ไม่ใช่รหัสผ่าน Mhan จะไม่ขอรหัสผ่าน Discord, Bot Token หรือ Apple ID

## ไอคอนถาดระบบและการเปิดพร้อม Windows

เมื่อเริ่มแชร์แล้ว Mhan จะบันทึก Application ID และตั้งให้เปิดพร้อม Windows หากกด **X** หน้าต่างจะซ่อนลงไปทำงานเบื้องหลังใกล้นาฬิกา ดับเบิลคลิกไอคอน Mhan เพื่อเปิดหน้าต่างอีกครั้ง คลิกขวาที่ไอคอนเพื่อหยุดแชร์ เปลี่ยนการตั้งค่า **เปิดเมื่อเข้า Windows** หรือออกจากโปรแกรม

ใช้ช่องติ๊ก **Apple Music** และ **Spotify** ในหน้าหลักเพื่อเลือกบริการที่จะแชร์ ส่วนช่อง **แชร์เนื้อเพลง** ควบคุม Lyrics Preview แยกต่างหาก ปิดได้โดยไม่หยุดแชร์เพลง หากเปิดเพลงทั้งสองบริการ Mhan จะเลือกเซสชันที่ Windows ส่งมาก่อน

## แก้ปัญหาเบื้องต้น

- **Discord ไม่แสดงเพลง:** เปิด Discord Desktop เปิดการแชร์กิจกรรมใน Activity Privacy และตรวจสอบว่า Mhan แจ้งว่ากำลังแชร์
- **ไม่มีเนื้อเพลง:** เปิดช่อง **แชร์เนื้อเพลง** เพลงต้องมี synced lyrics ใน LRCLIB หรือ LrcMux; plain lyrics ที่ไม่มีเวลา sync จะไม่แสดงเป็นบรรทัดปัจจุบัน และ Mhan จะลองใหม่เมื่อบริการขัดข้องชั่วคราว
- **ไม่พบเพลง:** เปิดเพลงใน Apple Music หรือ Spotify บนคอมเครื่องนี้ และติ๊กบริการนั้นใน Mhan เพลงที่หยุดหรือพักไว้จะไม่แสดงบน Discord
- **ไม่มีรูปปก:** โปรแกรมค้นหารูปจากแคตตาล็อก iTunes ของ Apple บางเพลงอาจค้นไม่พบ แต่ยังแสดงชื่อเพลงและศิลปินได้
- **Mhan เชื่อมต่อไม่ได้:** เปิด Discord Desktop ค้างไว้ Discord เวอร์ชันเว็บอย่างเดียวไม่รองรับการเชื่อมต่อ Rich Presence ในเครื่อง

## การอัปเดตอัตโนมัติ

Mhan ตรวจ GitHub Release ล่าสุดก่อนเริ่มแชร์เพลง และทุก 6 ชั่วโมงขณะเปิดโปรแกรม หากมี `Mhan.exe` รุ่นใหม่ โปรแกรมจะตรวจ SHA-256 ติดตั้งไว้ในโฟลเดอร์ตามชื่อรุ่นใหม่ อัปเดตตำแหน่งเปิดพร้อม Windows แล้วเปิดโปรแกรมใหม่ เมื่อรุ่นใหม่เปิดได้แล้วจะลบไฟล์และโฟลเดอร์ว่างของรุ่นเก่า หากติดตั้งไม่สำเร็จ โปรแกรมจะคืนไฟล์เดิมและเปิดรุ่นก่อนหน้า และจะไม่แทนที่รุ่นที่ติดตั้งด้วย Release ที่เก่ากว่า การอัปเดตเกิดเมื่อเผยแพร่ Release แล้วเท่านั้น การอัปโหลดซอร์สโค้ดอย่างเดียวยังไม่อัปเดตโปรแกรม

ระบบอัปเดตทำงานได้โดยไม่ต้องใช้ GitHub token เพราะ repository ของ Mhan เป็นสาธารณะ ในหน้า **ตั้งค่าอัปเดตอัตโนมัติ** สามารถตรวจการเชื่อมต่อ และใส่ fine-grained token แบบอ่านอย่างเดียวได้หากเปลี่ยน repository เป็นส่วนตัว token จะถูกเข้ารหัสด้วย DPAPI สำหรับบัญชี Windows นี้

## Build จากซอร์สโค้ด

ติดตั้ง .NET 10 SDK แล้วเปิด PowerShell ในโฟลเดอร์โปรเจกต์ จากนั้นรัน:

```powershell
dotnet build .\Mhan.csproj -c Release
```

## ความเป็นส่วนตัว

ระหว่างแชร์ Mhan อ่านชื่อเพลง ศิลปิน สถานะและตำแหน่งเพลงจาก Windows media session แล้วส่งกิจกรรมไปยัง Discord ค้นหารูปปกจาก iTunes และค้น synced lyrics จาก LRCLIB โดยใช้ LrcMux เป็นแหล่งสำรอง บริการ lyrics จะได้รับชื่อเพลง ศิลปิน และอัลบั้ม หากเปิดอัปเดตอัตโนมัติ Mhan จะเก็บ GitHub read-only token แบบเข้ารหัสสำหรับบัญชี Windows และส่งไปยัง GitHub API เท่านั้น โปรแกรมไม่เก็บหรือส่งรหัสผ่าน Discord หรือ Apple ID

# FMOD Studio Project Setup Guide — TOWER

> **Video tham khảo chính thức**: https://fmod.com/learn (playlist "Getting Started with FMOD Studio")
> **Docs chính thức**: https://fmod.com/docs/2.02/studio/

## Tổng quan

Tài liệu này hướng dẫn tạo FMOD Studio project cho game TOWER.
Dựa trên phân tích code ECS, project cần **7 Events** chia thành **2 Banks**.

---

## Bước 1: Tạo FMOD Studio Project

### Vị trí UI:
- Mở FMOD Studio
- **Menu bar** (thanh trên cùng) → `File` → `New Project`
- Một dialog "Save As" hiện ra → chọn folder `TOWER/FMODProject/` → đặt tên `TOWER.fspro` → Save

### Set Build Path:
- **Menu bar** → `Edit` → `Preferences` (hoặc Ctrl+,)
- Trong cửa sổ Preferences, chọn tab **"Build"** (bên trái)
- Mục **"Built Banks Output Directory"** → click `Browse` → trỏ đến:
  ```
  TOWER/Assets/StreamingAssets
  ```
- Click OK

---

## Bước 2: Import Audio Assets

### Vị trí UI:
- **Menu bar** → `Window` → `Asset Browser` (nếu chưa mở)
- Panel **"Assets"** sẽ hiện ở phía dưới hoặc bên phải (tùy layout)
- Trong panel Assets, **click chuột phải** vào vùng trống → `Import Assets...`
- Hoặc đơn giản hơn: **kéo thả** file từ Windows Explorer vào panel Assets

### Files cần import:

Mở folder `TOWER/Assets/Asset_Model/SoundEffects/` trong Windows Explorer, chọn tất cả file sau và kéo vào FMOD:

| File | Dùng cho |
|------|----------|
| `WeaponShot01.wav` → `WeaponShot07.wav` (7 files) | Weapon fire SFX |
| `MissileLaunch01.wav` | Missile launcher fire |
| `explosion-6055.mp3` | Explosion impact |
| `ServoLooped1.wav` | Heading rotation loop |
| `ServoLooped2.wav` | Elevation rotation loop |

> **Tip**: Chọn tất cả 11 files → kéo thả 1 lần vào FMOD Asset Browser.

---

## Bước 3: Tạo Folder Structure

### Vị trí UI:
- Nhìn panel bên **trái** — đó là **Events Browser** (danh sách events dạng tree)
- Nếu không thấy, vào **Menu bar** → `Window` → `Event Browser`
- **Click chuột phải** vào "Events" (root) → `New Folder`
- Đặt tên folder

### Tạo cấu trúc sau:

```
Events/
├── Weapons/          ← chuột phải Events → New Folder → "Weapons"
│   ├── Fire_Bullet
│   ├── Fire_Missile
│   └── Fire_Gatling
├── Impact/           ← chuột phải Events → New Folder → "Impact"
│   └── Explosion
└── Turret/           ← chuột phải Events → New Folder → "Turret"
    ├── Servo_Heading
    ├── Servo_Elevation
    └── Gatling_Spin
```

---

## Bước 4: Tạo Events

### Cách tạo Event (áp dụng cho tất cả):
1. Trong **Events Browser** (panel trái), **chuột phải** vào folder mong muốn (vd: "Weapons")
2. Chọn `New Event` → `3D Event` (cho tất cả events trong game này)
3. Đặt tên event (vd: "Fire_Bullet")
4. **Double-click** vào event vừa tạo để mở Event Editor (panel giữa lớn)

### Giao diện Event Editor:
```
┌─────────────────────────────────────────────────────┐
│  [Timeline ruler - thanh thời gian ở trên]          │
├─────────────────────────────────────────────────────┤
│  Audio 1    │ [Kéo audio vào đây]                   │  ← Audio Track
├─────────────────────────────────────────────────────┤
│  Master     │ [Effects chain]                       │  ← Master Track
└─────────────────────────────────────────────────────┘
│  [Deck/Properties panel ở dưới cùng]                │
└─────────────────────────────────────────────────────┘
```

---

### 4.1 — `event:/Weapons/Fire_Bullet` (One-shot, 3D)

**Tạo Multi Instrument** (random clips mỗi lần bắn):
1. Mở event "Fire_Bullet" (double-click)
2. Trên **Audio Track** (dòng "Audio 1"), **chuột phải** vào vùng timeline → `Add Multi Instrument`
   - Một khối màu xuất hiện trên timeline
3. **Click** vào khối Multi Instrument đó → panel dưới cùng hiện **Playlist**
4. Từ **Asset Browser** (panel dưới/phải), **kéo thả** `WeaponShot01.wav` → `WeaponShot07.wav` vào Playlist
5. Trong Playlist panel, set:
   - **Playlist Mode**: `Shuffle` (dropdown ở góc trên playlist)
   - Tick **"Avoid Repeating Last"**: 2

**Thêm Spatializer (3D sound)**:
1. Click vào **Master Track** (dòng "Master" bên dưới Audio Track)
2. Nhìn panel dưới cùng → tab **"Effects"** (hoặc "Deck")
3. Nếu chưa có Spatializer: **chuột phải** trong Effects area → `Add Effect` → `FMOD Spatializer`
4. Click vào Spatializer effect → set:
   - **Min Distance**: `1` (m)
   - **Max Distance**: `50` (m)
   - **Envelopment**: để mặc định

> **Note**: Event 3D tự động có Spatializer. Nếu đã có thì chỉ cần chỉnh Min/Max.

---

### 4.2 — `event:/Weapons/Fire_Missile` (One-shot, 3D)

1. Tạo event trong folder "Weapons", tên "Fire_Missile"
2. Trên Audio Track, **chuột phải** → `Add Single Instrument`
3. Kéo `MissileLaunch01.wav` từ Asset Browser vào instrument
4. Spatializer: Min `2m`, Max `80m`

---

### 4.3 — `event:/Weapons/Fire_Gatling` (One-shot, 3D)

1. Giống Fire_Bullet nhưng chỉ kéo `WeaponShot01-03.wav` (3 clips ngắn)
2. Hoặc: chuột phải Fire_Bullet → `Duplicate` → rename → xóa bớt clips
3. Giảm volume Master Track: -3dB (gatling bắn liên tục nên cần nhẹ hơn)

---

### 4.4 — `event:/Impact/Explosion` (One-shot, 3D)

1. Tạo event trong folder "Impact", tên "Explosion"
2. Add Single Instrument → kéo `explosion-6055.mp3` vào
3. Spatializer: Min `3m`, Max `100m`
4. Optional — thêm pitch randomization:
   - Click vào instrument → panel dưới → **"Pitch"** section
   - Set **Pitch Randomization**: `±1` semitone

---

### 4.5 — `event:/Turret/Servo_Heading` (Looping, 3D, Parameter-driven)

**⚠️ Event này phức tạp hơn — có Loop + Parameter**

**Bước A: Tạo event + instrument**
1. Tạo event trong folder "Turret", tên "Servo_Heading"
2. Add Single Instrument → kéo `ServoLooped1.wav` vào

**Bước B: Set Loop**
1. Click vào instrument (khối trên timeline)
2. Panel dưới → tab **"Trigger Behavior"** hoặc properties
3. Tìm **"Loop"** → bật ON
4. HOẶC: trên timeline, kéo **Loop Region** (thanh xanh lá ở trên ruler):
   - Chuột phải trên timeline ruler → `Add Loop Region`
   - Kéo loop region bao phủ toàn bộ instrument

**Bước C: Thêm Parameter**
1. Nhìn phía **dưới timeline** — có thanh **"Parameter"** (hoặc sheet tabs)
2. **Chuột phải** vào vùng parameter → `Add Parameter Sheet`
3. Dialog hiện ra:
   - **Name**: `SpeedHeadingFactor`
   - **Type**: `User` (Continuous)
   - **Minimum**: `0`
   - **Maximum**: `1`
   - **Default**: `0`
4. Click OK

**Bước D: Automation (optional nhưng recommended)**
1. Sau khi tạo parameter, bạn thấy 1 tab mới tên "SpeedHeadingFactor" ở dưới timeline
2. Click vào tab đó → timeline chuyển sang parameter view (trục X = parameter value)
3. Click vào **Master Track** → trong Effects deck, **chuột phải** vào Volume knob → `Add Automation`
4. Vẽ curve Volume:
   - Điểm trái (value=0): Volume = **-∞ dB** (kéo xuống đáy)
   - Điểm giữa (value=0.3): Volume = **-12 dB**
   - Điểm phải (value=1): Volume = **0 dB** (kéo lên top)
5. Thêm **Pitch Shifter** effect: chuột phải trong Effects deck → Add Effect → Pitch Shifter
6. Chuột phải vào Pitch Shifter knob → `Add Automation`
7. Vẽ curve Pitch Shifter (đơn vị là **multiplier**, range 0.5x → 2.0x):
   - Value 0 → Pitch Shifter = **0.8x** (chậm = pitch thấp hơn)
   - Value 1 → Pitch Shifter = **1.0x** (nhanh = pitch gốc)

> **LƯU Ý**: Pitch Shifter trong FMOD Studio dùng **multiplier** (0.5x = xuống 1 octave, 1.0x = không đổi, 2.0x = lên 1 octave), KHÔNG phải cents.

**Spatializer**: Min `0.5m`, Max `20m`

---

### 4.6 — `event:/Turret/Servo_Elevation` (Looping, 3D, Parameter-driven)

**Giống hệt Servo_Heading** nhưng:
- Dùng `ServoLooped2.wav`
- Parameter name: `SpeedElevationFactor` (range 0→1)

> **Shortcut**: Chuột phải Servo_Heading → Duplicate → rename → đổi audio file + parameter name

---

### 4.7 — `event:/Turret/Gatling_Spin` (Looping, 3D, Parameter-driven)

**Setup giống Servo_Heading** nhưng:
- Audio: dùng tạm `ServoLooped1.wav` (hoặc tìm motor/whirring sound)
- Parameter name: `SpeedGatlingSpinSpeedFactor` (range 0→1)
- Automation khác:
  - Volume: 0→-∞ dB (silent), 0.1→-20dB, 1.0→0dB
  - Pitch Shifter: 0→**0.7x**, 1.0→**1.2x** (spin nhanh = pitch cao hơn nhiều)

---

## Bước 5: Tạo Banks

### Vị trí UI:
- Nhìn **thanh tab** phía trên Events Browser (panel trái) — có các tab: **Events | Banks | ...**
- Click tab **"Banks"**
- Hoặc: **Menu bar** → `Window` → `Banks`

### Tạo Banks:
1. Trong Banks panel, bạn đã thấy **"Master"** bank (tự tạo sẵn) — KHÔNG XÓA
2. **Chuột phải** vào vùng trống → `New Bank` → đặt tên `Weapons`
3. **Chuột phải** → `New Bank` → đặt tên `Turret_Loops`

### Assign Events vào Banks:

**Cách 1** (kéo thả):
- Chuyển về tab **Events** (panel trái)
- **Kéo** event "Fire_Bullet" → **thả** vào bank "Weapons" trong Banks panel

**Cách 2** (Properties):
- **Click** vào event (vd: Fire_Bullet)
- Nhìn panel **Properties** (thường ở bên phải hoặc dưới)
- Tìm mục **"Bank"** → dropdown → chọn `Weapons`

### Phân bổ:

| Bank Name | Events |
|-----------|--------|
| `Master` | (metadata only — tự động) |
| `Weapons` | Fire_Bullet, Fire_Missile, Fire_Gatling, Explosion |
| `Turret_Loops` | Servo_Heading, Servo_Elevation, Gatling_Spin |

---

## Bước 6: Build Banks

### Vị trí UI:
- **Menu bar** → `File` → `Build` (hoặc phím tắt **F7**)
- Hoặc: **Menu bar** → `File` → `Build All Platforms`

### Kiểm tra output:
- Mở Windows Explorer → navigate đến `TOWER/Assets/StreamingAssets/`
- Phải thấy các file:
  ```
  Master.bank
  Master.strings.bank
  Weapons.bank
  Turret_Loops.bank
  ```

> Nếu không thấy folder StreamingAssets, kiểm tra lại Build path ở Bước 1.

---

## Bước 7: Unity Integration

### 7.1 — Cấu hình FMOD Settings trong Unity

**Vị trí UI trong Unity**:
- **Menu bar Unity** → `FMOD` → `Edit Settings`
- Hoặc: `Edit` → `Project Settings` → scroll xuống tìm **"FMOD"** bên trái

**Trong FMOD Settings Inspector**:
1. **Source Type**: chọn `FMOD Studio Project` (dropdown đầu tiên)
2. **Studio Project Path**: click `Browse` → trỏ đến `FMODProject/TOWER.fspro`
3. **Bank Output Path**: để mặc định `Assets/StreamingAssets` (hoặc set nếu khác)
4. Click **"Refresh Banks"** button (nếu có)

### 7.2 — Thêm FMOD Listener

**QUAN TRỌNG** — Không có Listener = không nghe gì:
1. Tìm **Main Camera** trong Hierarchy
2. **Add Component** → search "FMOD Studio Listener"
3. Xóa component **"Audio Listener"** mặc định của Unity (nếu có)

### 7.3 — Assign Event References trên Prefabs

Mở các prefab turret/weapon trong Inspector:

| Component trên Prefab | Field trong Inspector | Chọn Event |
|-----------|-------|------------|
| `SoundWeaponEffectShootAuthoring` | soundEventReference... | `event:/Weapons/Fire_Bullet` hoặc `Fire_Missile` |
| `ExplosionSFXAuthoring` | soundEventReference... | `event:/Impact/Explosion` |
| `SFX_HeadingAuthoring` | soundEventReference | `event:/Turret/Servo_Heading` |
| `SFX_ElevationAuthoring` | soundEventReference | `event:/Turret/Servo_Elevation` |
| `SFX_GatlingSpinAuthoring` | soundEventReference... | `event:/Turret/Gatling_Spin` |

**Cách chọn Event Reference trong Inspector**:
- Click vào field `EventReference` → một popup **"FMOD Event Browser"** hiện ra
- Navigate theo folder structure: Weapons → Fire_Bullet
- Hoặc search bằng tên
- Click chọn → Done

---

## Bước 8: Verify

1. Unity → Play mode
2. Mở **Console** (Window → Console) — kiểm tra không có FMOD errors (đỏ)
3. Test:
   - Turret xoay → nghe servo sound (pitch thay đổi theo speed)
   - Bắn → nghe weapon fire
   - Đạn hit enemy → nghe explosion
   - Gatling spin up → nghe motor sound tăng dần

### Debug trong FMOD Studio:
- Trong FMOD Studio, vào **Menu** → `Window` → `Profiler`
- Click **"Connect"** (kết nối với Unity đang Play)
- Bạn sẽ thấy real-time: events đang play, CPU usage, instance count

---

## Phụ lục: Thông số chi tiết cho từng Event

### A. Servo_Heading & Servo_Elevation

**Spatializer:**
- Min Distance: `0.5 m`
- Max Distance: `20 m`
- Doppler: `Off` (servo không di chuyển nhanh)

**Volume Automation** (theo SpeedHeadingFactor / SpeedElevationFactor):
| Parameter Value | Volume (dB) |
|:-:|:-:|
| 0.00 | -∞ (silent) |
| 0.05 | -40 dB |
| 0.15 | -20 dB |
| 0.30 | -12 dB |
| 0.50 | -6 dB |
| 0.75 | -3 dB |
| 1.00 | 0 dB |

> Curve shape: **Logarithmic** (tăng nhanh ở đầu, chậm dần ở cuối)

**Pitch Shifter Automation:**
| Parameter Value | Pitch Multiplier |
|:-:|:-:|
| 0.00 | 0.85x |
| 0.50 | 0.92x |
| 1.00 | 1.00x |

> Curve shape: **Linear**

**Low-pass Filter — dùng Multiband EQ hoặc Three EQ:**

**Cách 1: Three EQ** (đơn giản, recommend):
1. Add Effect → `Three EQ`
2. Set **High Crossover**: `2000 Hz` (cố định)
3. Chuột phải **High Gain** knob → `Add Automation`
4. Vẽ curve:

| Parameter Value | High Gain |
|:-:|:-:|
| 0.00 | -12 dB (đục, muffle) |
| 0.30 | -6 dB |
| 0.60 | -3 dB |
| 1.00 | 0 dB (sáng, full) |

**Cách 2: Multiband EQ** (chính xác hơn):
1. Add Effect → `Multiband EQ`
2. Chỉ dùng **Band A** — set các band khác Gain = 0
3. Band A settings:
   - **Filter Type**: `Lowpass 12dB` (chọn trong dropdown)
   - **Q**: `0.707`
   - **Gain**: `0 dB` (để mặc định)
4. Chuột phải **Frequency** knob của Band A → `Add Automation`
5. Vẽ curve Frequency:

| Parameter Value | Lowpass Frequency |
|:-:|:-:|
| 0.00 | 800 Hz (chỉ nghe bass, rất đục) |
| 0.30 | 2000 Hz |
| 0.60 | 5000 Hz |
| 1.00 | 22000 Hz (mở hết, full sound) |

> **Giải thích**: Lowpass filter cắt tất cả tần số TRÊN giá trị Frequency. Frequency thấp = âm đục. Frequency cao = âm sáng.

---

### B. Gatling_Spin

**Spatializer:**
- Min Distance: `1 m`
- Max Distance: `30 m`
- Doppler: `Off`

**Volume Automation** (theo SpeedGatlingSpinSpeedFactor):
| Parameter Value | Volume (dB) |
|:-:|:-:|
| 0.00 | -∞ (silent) |
| 0.05 | -30 dB |
| 0.10 | -18 dB |
| 0.30 | -10 dB |
| 0.50 | -5 dB |
| 0.75 | -2 dB |
| 1.00 | 0 dB |

**Pitch Shifter Automation:**
| Parameter Value | Pitch Multiplier |
|:-:|:-:|
| 0.00 | 0.60x |
| 0.25 | 0.75x |
| 0.50 | 0.90x |
| 0.75 | 1.05x |
| 1.00 | 1.20x |

> Curve shape: **Linear** hoặc hơi S-curve

**Low-pass Filter** (Three EQ):
- High Crossover: `3000 Hz`
- Automate High Gain:

| Parameter Value | High Gain |
|:-:|:-:|
| 0.00 | -18 dB |
| 0.30 | -10 dB |
| 0.60 | -4 dB |
| 1.00 | 0 dB |

Hoặc **Multiband EQ** Band A Lowpass:

| Parameter Value | Lowpass Frequency |
|:-:|:-:|
| 0.00 | 500 Hz |
| 0.30 | 1500 Hz |
| 0.60 | 4000 Hz |
| 1.00 | 22000 Hz |

---

### C. Fire_Bullet

**Spatializer:**
- Min Distance: `1 m`
- Max Distance: `50 m`
- Doppler: `Off`

**Multi Instrument Settings:**
- Playlist Mode: `Shuffle`
- Avoid Repeating Last: `2`

**Randomization** (trên mỗi instrument trong playlist):
| Property | Min | Max |
|:-:|:-:|:-:|
| Pitch | -2 semitones | +2 semitones |
| Volume | -1 dB | +1 dB |

> Cách set: Click instrument → panel dưới → "Pitch" section → kéo randomization range

---

### D. Fire_Missile

**Spatializer:**
- Min Distance: `2 m`
- Max Distance: `80 m`
- Doppler: `Off`

**Randomization:**
| Property | Min | Max |
|:-:|:-:|:-:|
| Pitch | -0.5 semitones | +0.5 semitones |
| Volume | -0.5 dB | +0.5 dB |

---

### E. Fire_Gatling

**Spatializer:**
- Min Distance: `1 m`
- Max Distance: `40 m`
- Doppler: `Off`

**Multi Instrument Settings:**
- Playlist Mode: `Shuffle`
- Avoid Repeating Last: `1`

**Randomization:**
| Property | Min | Max |
|:-:|:-:|:-:|
| Pitch | -1 semitone | +1 semitone |
| Volume | -1.5 dB | +1.5 dB |

**Master Track Volume:** `-3 dB` (gatling bắn liên tục nên cần nhẹ hơn bullet)

---

### F. Explosion

**Spatializer:**
- Min Distance: `3 m`
- Max Distance: `100 m`
- Doppler: `Off`

**Randomization:**
| Property | Min | Max |
|:-:|:-:|:-:|
| Pitch | -1 semitone | +1 semitone |
| Volume | -1 dB | +1 dB |

**Optional Effects trên Master Track:**
| Effect | Setting |
|:-:|:-:|
| Compressor | Threshold: -10 dB, Ratio: 4:1, Attack: 1ms, Release: 100ms |
| Three EQ | Low Gain: +2 dB (thêm bass punch) |

---

## Mapping Code ↔ FMOD Parameters

| Code (setParameterByName) | FMOD Event | Parameter Name | Range |
|---------------------------|------------|----------------|-------|
| `PlayAndStopSoundHeadingSystem` | Servo_Heading | `SpeedHeadingFactor` | 0-1 |
| `PlayAndStopSoundElevationSystem` | Servo_Elevation | `SpeedElevationFactor` | 0-1 |
| `PlayAndStopSoundGatlingSpinSystem` | Gatling_Spin | `SpeedGatlingSpinSpeedFactor` | 0-1 |

---

## Audio Files Cần Thêm (Optional)

| Sound | Gợi ý | Dùng cho |
|-------|-------|----------|
| Gatling spin loop | Motor/whirring sound | Gatling_Spin event |
| Bullet impact (non-explosive) | Metal ping/ricochet | Bullet hit enemy (chưa implement) |
| Building placement | Click/thud | Khi đặt building |
| UI sounds | Click, hover | UI interactions |
| Ambient | Wind, environment | Background atmosphere |

---

## Tài liệu tham khảo có hình ảnh

| Nội dung | Link |
|----------|------|
| Video Getting Started (có UI walkthrough) | https://fmod.com/learn |
| Docs: Creating Events | https://fmod.com/docs/2.02/studio/working-with-instruments.html |
| Docs: Parameters | https://fmod.com/docs/2.02/studio/parameters.html |
| Docs: Automation | https://fmod.com/docs/2.02/studio/automation-and-modulation.html |
| Docs: Banks | https://fmod.com/docs/2.02/studio/banks.html |
| Docs: Unity Integration | https://fmod.com/docs/2.02/unity/ |
| Docs: Multiband EQ | https://fmod.com/docs/2.02/studio/effects-reference.html |

---

## Tips

- **Multi Instrument** cho weapon fire = mỗi lần bắn random 1 clip khác nhau → tránh repetitive
- **Scatterer Instrument** cho ambient = tự động play random clips theo interval
- **Parameter Automation** > Code control: sound designer có thể tune mà không cần rebuild code
- **Snapshot** cho slow-mo/pause: tạo snapshot event, trigger từ code khi cần
- **Profiler** (Window → Profiler): monitor CPU usage, instance count real-time

---

## Troubleshooting

| Vấn đề | Giải pháp |
|--------|-----------|
| "Bank not found" | Kiểm tra Build path = StreamingAssets, rebuild banks |
| "Event not found" | Kiểm tra event path chính xác, rebuild banks |
| Sound không 3D | Kiểm tra Spatializer effect trên Master Track |
| Sound quá nhỏ/lớn | Adjust Min/Max distance trong Spatializer |
| Looping sound không stop | Kiểm tra code gọi `StopSoundEffectLoop()` đúng |

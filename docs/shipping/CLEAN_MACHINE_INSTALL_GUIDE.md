# Crown & Conquest — Clean-Machine Installation & Deployment Guide

## 1. System Requirements

### Minimum Hardware Specifications:
- **Processor:** 64-bit x64 Dual-Core CPU @ 2.0 GHz or higher
- **Memory:** 2 GB RAM minimum (4 GB recommended)
- **Graphics:** DirectX 11 / Vulkan compatible GPU (or Software/Headless fallback)
- **Storage:** 1 GB available disk space
- **Operating System:** Windows 10 (64-bit) / Windows 11 (64-bit)

### Recommended Hardware Specifications:
- **Processor:** Quad-Core Intel Core i5 / AMD Ryzen 5 @ 3.2 GHz or higher
- **Memory:** 8 GB RAM
- **Graphics:** NVIDIA GeForce GTX 1060 / AMD Radeon RX 580 or higher
- **Storage:** Solid State Drive (SSD) with 2 GB free

---

## 2. Clean-Machine Installation Steps

1. **Extract Distribution Archive:**
   Extract `CrownConquest_v1.0.0_win-x64.zip` to your chosen local directory (e.g., `C:\Games\CrownConquest\`).

2. **Verify Cryptographic Package Integrity:**
   Open PowerShell and compute SHA-256 hashes against `SHA256SUMS.txt`:
   ```powershell
   Get-FileHash CrownConquest.exe -Algorithm SHA256
   ```

3. **Validate Environment:**
   Run the self-diagnostic check before first launch:
   ```powershell
   .\CrownConquest.exe --validate-env
   ```

4. **Launch the Game:**
   Double-click `CrownConquest.exe` or execute from command prompt.

---

## 3. Troubleshooting & Frequently Asked Questions

- **Issue:** Game immediately exits with code `4`.
  - **Solution:** Verify your operating system is 64-bit. 32-bit platforms are not supported.
- **Issue:** Headless test fails to initialize sound drivers.
  - **Solution:** Run with `--headless` switch which automatically routes audio to the null/headless audio sink.
- **Issue:** Save games fail to write.
  - **Solution:** Ensure the installation directory or `%USERPROFILE%\Saved Games\CrownConquest\` has write permissions and at least 50 MB of free storage.

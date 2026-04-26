# ludus_win_privesc

An Ansible role for [Ludus](https://ludus.cloud/) that deploys **30 Windows privilege escalation lab scenarios** on Windows 10/11 and Windows Server 2019/2022. Each scenario is individually toggle-able and idempotent.

Scenarios cover token privileges, service misconfigurations, registry weaknesses, credential discovery, and path/file vulnerabilities — providing a realistic environment for practicing local privilege escalation techniques.

---

## Scenarios Covered

All 30 scenarios are individually toggle-able via boolean variables:

| Category | Scenario | Variable |
|---|---|---|
| **Service** | Unquoted Service Path | `ludus_win_privesc_unquoted_service_path` |
| **Service** | Weak Service Binary Permissions | `ludus_win_privesc_weak_service_binary` |
| **Service** | Weak Service Registry Permissions | `ludus_win_privesc_weak_service_registry` |
| **Credential Discovery** | Hardcoded Credentials in .NET Binary | `ludus_win_privesc_hardcoded_credentials_dotnet` |
| **Credential Discovery** | Stored Credentials in Winlogon Registry | `ludus_win_privesc_stored_credentials_winlogon` |
| **Credential Discovery** | Leaked Credentials in PowerShell History | `ludus_win_privesc_leaked_creds_ps_history` |
| **Credential Discovery** | Leaked Credentials in Git Config | `ludus_win_privesc_leaked_creds_git` |
| **Credential Discovery** | Credentials in Answer Files (Unattend.xml) | `ludus_win_privesc_answer_files` |
| **Credential Discovery** | GPP Cached Password (cpassword) | `ludus_win_privesc_gpp_cpassword` |
| **Credential Discovery** | WiFi Profile with Saved Password | `ludus_win_privesc_wifi_profile` |
| **Credential Discovery** | SSH Private Keys (unprotected) | `ludus_win_privesc_ssh_private_keys` |
| **Credential Discovery** | Office Documents with Embedded Credentials | `ludus_win_privesc_office_creds` |
| **Autorun** | Weak Startup Folder Permissions | `ludus_win_privesc_weak_startup_folder` |
| **Autorun** | Weak Registry Run Key Permissions | `ludus_win_privesc_weak_registry_run_key` |
| **Elevation** | AlwaysInstallElevated (MSI) | `ludus_win_privesc_always_install_elevated` |
| **Elevation** | UAC Bypass Setup (dedicated uacuser) | `ludus_win_privesc_uac_bypass_setup` |
| **Token Privileges** | SeImpersonatePrivilege (IIS webshell) | `ludus_win_privesc_seimpersonate` |
| **Token Privileges** | SeBackupPrivilege | `ludus_win_privesc_sebackup` |
| **Token Privileges** | SeDebugPrivilege | `ludus_win_privesc_sedebug` |
| **Token Privileges** | SeRestorePrivilege | `ludus_win_privesc_serestore` |
| **Token Privileges** | SeTakeOwnershipPrivilege | `ludus_win_privesc_setakeownership` |
| **Token Privileges** | SeLoadDriverPrivilege | `ludus_win_privesc_seloaddriver` |
| **Token Privileges** | SeManageVolumePrivilege | `ludus_win_privesc_semanagevolume` |
| **Privileged Groups** | Backup Operators (SeBackup+SeRestore auto-granted) | `ludus_win_privesc_backup_operators_group` |
| **Privileged Groups** | Server Operators (domain-joined only) | `ludus_win_privesc_server_operators_group` |
| **Registry Misconfig** | WDigest Enabled (plaintext creds in LSASS) | `ludus_win_privesc_wdigest_enabled` |
| **Registry Misconfig** | WSUS over HTTP (MITM update injection) | `ludus_win_privesc_wsus_http` |
| **Registry Misconfig** | AppLocker Disabled / Weak Rules | `ludus_win_privesc_applocker_disabled` |
| **Path / File** | DLL Hijacking via Writable PATH Directory | `ludus_win_privesc_dll_hijacking_path` |

---

## ⚠️ Tamper Protection 

To remove defender you have to disable Tamper Protection and rerun the role.


---

## Token Privileges — whoami /priv Explained

Token privileges appear as `Disabled` in `whoami /priv` until activated. This is
normal Windows behavior — **Disabled means present but inactive**, not missing.

```
SeDebugPrivilege    Debug programs    Disabled
```

All exploitation tools activate privileges automatically via `AdjustTokenPrivileges()`
before using them. `procdump`, `mimikatz`, `reg save` etc. all do this internally.

**If the victim user is in the local Administrators group**, UAC filters the token
to Medium Integrity and removes most privileges from the visible token. The role
intentionally uses a **dedicated `uacuser`** for the UAC bypass scenario so that
`lowpriv` remains a plain user and all token privileges are visible directly
via `whoami /priv` without elevation.

---

## Requirements

- Target must be a Windows host (Server 2019/2022 or Windows 10/11)
- WinRM must be configured (Ludus handles this automatically)
- Chocolatey must be installed on the target (required for the git credential leak scenario)
- For domain user mode: the host must be domain-joined
- **Tamper Protection must be disabled** 

---

## Domain User Creation -> not tested

When `ludus_win_privesc_victim_user_type: domain`, the role can automatically
create the victim user in Active Directory. The task runs **on the DC** — the
role must also be assigned to the DC VM with
`ludus_win_privesc_create_domain_user: true` and all scenarios set to `false`.

```yaml
# On DC: only creates the domain user (all scenarios disabled)
- vm_name: "{{ range_id }}-DC01"
  roles:
    - name: ludus_win_privesc
      vars:
        ludus_win_privesc_victim_user_type: "domain"
        ludus_win_privesc_victim_user: "lowpriv"
        ludus_win_privesc_victim_password: "Password123!"
        ludus_win_privesc_domain: "LAB"
        ludus_win_privesc_domain_fqdn: "lab.local"
        ludus_win_privesc_create_domain_user: true
        # all scenarios: false

# On member VMs: all scenarios run, user creation skipped
- vm_name: "{{ range_id }}-WIN2022"
  roles:
    - name: ludus_win_privesc
      vars:
        ludus_win_privesc_victim_user_type: "domain"
        ludus_win_privesc_create_domain_user: false
        # all scenarios: true
```

| Variable | Default | Description |
|---|---|---|
| `ludus_win_privesc_create_domain_user` | `true` | Create victim user in AD (DC only) |
| `ludus_win_privesc_domain_fqdn` | `lab.local` | FQDN of the target domain |
| `ludus_win_privesc_victim_ou` | `""` | OU for new user (empty = default Users container) |

---

## Role Variables

| Variable | Default | Description |
|---|---|---|
| **User Configuration** | | |
| `ludus_win_privesc_victim_user_type` | `local` | `local` or `domain` |
| `ludus_win_privesc_victim_user` | `lowpriv` | Username for the low-privilege victim user |
| `ludus_win_privesc_victim_password` | `Password123!` | Password for local victim user |
| `ludus_win_privesc_domain` | auto-detected | NetBIOS domain name |
| `ludus_win_privesc_admin_user` | `Administrator` | Admin username |
| `ludus_win_privesc_admin_password` | `P@ssw0rd123!` | Admin password |
| **Defender** | | |
| `ludus_win_privesc_disable_defender` | `true` | Disable Windows Defender via registry |
| `ludus_win_privesc_install_defender_gui` | `false` | Install Defender GUI on Server (requires reboot) |
| `ludus_win_privesc_reboot_after_privs` | `true` | Reboot after granting token privileges |
| **UAC** | | |
| `ludus_win_privesc_uac_password` | `UacBypass123!` | Password for the dedicated UAC bypass user (`uacuser`) |
| **Original Scenarios** | | |
| `ludus_win_privesc_seimpersonate` | `true` | SeImpersonatePrivilege + IIS webshell |
| `ludus_win_privesc_sebackup` | `true` | SeBackupPrivilege |
| `ludus_win_privesc_unquoted_service_path` | `true` | Unquoted service path |
| `ludus_win_privesc_weak_service_binary` | `true` | Weak service binary permissions |
| `ludus_win_privesc_weak_service_registry` | `true` | Weak service registry permissions |
| `ludus_win_privesc_hardcoded_credentials_dotnet` | `true` | Hardcoded creds in .NET binary |
| `ludus_win_privesc_stored_credentials_winlogon` | `true` | Autologon creds in registry |
| `ludus_win_privesc_leaked_creds_ps_history` | `true` | Creds in PowerShell history |
| `ludus_win_privesc_leaked_creds_git` | `true` | Creds in Git config |
| `ludus_win_privesc_answer_files` | `true` | Creds in Unattend.xml |
| `ludus_win_privesc_always_install_elevated` | `true` | AlwaysInstallElevated |
| `ludus_win_privesc_weak_startup_folder` | `true` | Writable startup folder |
| `ludus_win_privesc_weak_registry_run_key` | `true` | Writable Run key |
| `ludus_win_privesc_uac_bypass_setup` | `true` | UAC bypass lab (creates `uacuser`) |
| **Token Privileges** | | |
| `ludus_win_privesc_sedebug` | `true` | SeDebugPrivilege |
| `ludus_win_privesc_serestore` | `true` | SeRestorePrivilege |
| `ludus_win_privesc_setakeownership` | `true` | SeTakeOwnershipPrivilege |
| `ludus_win_privesc_seloaddriver` | `true` | SeLoadDriverPrivilege |
| `ludus_win_privesc_semanagevolume` | `true` | SeManageVolumePrivilege |
| `ludus_win_privesc_backup_operators_group` | `true` | Add victim to Backup Operators |
| `ludus_win_privesc_server_operators_group` | `true` | Add victim to Server Operators (domain-joined only) |
| **Registry Misconfigs** | | |
| `ludus_win_privesc_wdigest_enabled` | `true` | Enable WDigest plaintext caching |
| `ludus_win_privesc_wsus_http` | `true` | WSUS over HTTP |
| `ludus_win_privesc_applocker_disabled` | `true` | Disable AppLocker enforcement |
| `ludus_win_privesc_applocker_weak_rules` | `false` | Deploy weak AppLocker Default Rules |
| **Path / File** | | |
| `ludus_win_privesc_dll_hijacking_path` | `true` | Writable directory in SYSTEM PATH |
| `ludus_win_privesc_dll_hijack_dir` | `C:\DevUtils` | Directory to create and add to PATH |
| `ludus_win_privesc_gpp_cpassword` | `true` | GPP cpassword in fake Groups.xml |
| `ludus_win_privesc_wifi_profile` | `true` | WiFi profile with saved password |
| `ludus_win_privesc_ssh_private_keys` | `true` | Unprotected SSH private keys |
| `ludus_win_privesc_office_creds` | `true` | Office documents with embedded credentials |

---

## Dependencies

None.

---

## Example Ludus Range Config — Standalone (local users, 3 VMs)

Deploys three VMs for comprehensive privilege escalation testing:

- **WIN2022** — Windows Server 2022, all 30 scenarios
- **WIN11** — Windows 11, all 30 scenarios (DISM IIS path, locale-safe groups)
- **WIN11-UAC** — Windows 11, dedicated UAC bypass testing:
  - `lowpriv` is a plain Users member — `whoami /priv` shows all token privileges directly
  - `uacuser` is a local Administrator with UAC enabled — must bypass UAC to reach High Integrity


```yaml
---

ludus:

  # ── Kali Attack Box ──────────────────────────────────────────────────────────
  - vm_name: "{{ range_id }}-KALI"
    hostname: "{{ range_id }}-kali"
    template: kali-x64-desktop-template
    vlan: 10
    ip_last_octet: 99
    ram_gb: 4
    cpus: 2
    linux: true

  # ── Windows Server 2022 Target ───────────────────────────────────────────────
  # Tests: Install-WindowsFeature (IIS), all service misconfigs, secedit privs
  - vm_name: "{{ range_id }}-WIN2022"
    hostname: "WIN2022"
    template: win2022-server-x64-template
    vlan: 10
    ip_last_octet: 50
    ram_gb: 4
    cpus: 2
    windows:
      sysprep: true
    roles:
      - name: ludus_win_privesc_new
        vars:
          # ── User config ──────────────────────────────────────────────────────
          ludus_win_privesc_victim_user_type: "local"
          ludus_win_privesc_victim_user: "lowpriv"
          ludus_win_privesc_victim_password: "Password123!"
          ludus_win_privesc_admin_user: "Administrator"
          ludus_win_privesc_admin_password: "P@ssw0rd123!"
          ludus_win_privesc_create_domain_user: false  # local mode — no AD user needed

          ludus_win_privesc_install_defender_gui: false    # ← keep false after GUI install
          ludus_win_privesc_disable_defender: true
          # ── Original Scenarios ───────────────────────────────────────────────
          ludus_win_privesc_seimpersonate: true     
          ludus_win_privesc_sebackup: true
          ludus_win_privesc_unquoted_service_path: true
          ludus_win_privesc_weak_service_binary: true
          ludus_win_privesc_weak_service_registry: true
          ludus_win_privesc_hardcoded_credentials_dotnet: true
          ludus_win_privesc_stored_credentials_winlogon: true
          ludus_win_privesc_leaked_creds_ps_history: true
          ludus_win_privesc_leaked_creds_git: true
          ludus_win_privesc_answer_files: true
          ludus_win_privesc_always_install_elevated: true
          ludus_win_privesc_weak_startup_folder: true
          ludus_win_privesc_weak_registry_run_key: true
          ludus_win_privesc_uac_bypass_setup: false  # handled on WIN11-UAC VM

          # ── Group A: Token Privileges ─────────────────────────────────────────
          ludus_win_privesc_sedebug: true
          ludus_win_privesc_serestore: true
          ludus_win_privesc_setakeownership: true
          ludus_win_privesc_seloaddriver: true
          ludus_win_privesc_semanagevolume: true
          ludus_win_privesc_backup_operators_group: true
          ludus_win_privesc_server_operators_group: true

          # ── Group B: Registry / Security Config ───────────────────────────────
          ludus_win_privesc_wdigest_enabled: true
          ludus_win_privesc_wsus_http: true
          ludus_win_privesc_applocker_disabled: true
          ludus_win_privesc_applocker_weak_rules: false

          # ── Group C: Path / File ──────────────────────────────────────────────
          ludus_win_privesc_dll_hijacking_path: true
          ludus_win_privesc_dll_hijack_dir: "C:\\DevUtils"
          ludus_win_privesc_gpp_cpassword: true
          ludus_win_privesc_wifi_profile: true
          ludus_win_privesc_ssh_private_keys: true
          ludus_win_privesc_office_creds: true

  # ── Windows 11 22H2 Target ───────────────────────────────────────────────────
  # Tests: DISM IIS path, IIS-ASPNET fallback, SID-based group membership,
  #        Get-CimInstance. Win11 hat Defender GUI immer — TP via RDP deaktivieren.
  - vm_name: "{{ range_id }}-WIN11"
    hostname: "WIN11"
    template: win11-22h2-x64-enterprise-template
    vlan: 10
    ip_last_octet: 51
    ram_gb: 4
    cpus: 2
    windows:
      sysprep: true
    roles:
      - name: ludus_win_privesc_new
        vars:
          # ── User config ──────────────────────────────────────────────────────
          ludus_win_privesc_victim_user_type: "local"
          ludus_win_privesc_victim_user: "lowpriv"
          ludus_win_privesc_victim_password: "Password123!"
          ludus_win_privesc_admin_user: "Administrator"
          ludus_win_privesc_admin_password: "P@ssw0rd123!"
          ludus_win_privesc_create_domain_user: false  # local mode — no AD user needed

          ludus_win_privesc_install_defender_gui: false  # not needed on Win11
          ludus_win_privesc_disable_defender: true
          ludus_win_privesc_reboot_after_privs: true  # false = 1 less reboot, whoami /priv needs manual re-login

          # ── Original Scenarios ────────────────────────────────────────────────
          ludus_win_privesc_seimpersonate: true       # ⚠️ requires TP off (Defender blocks IIS install)
          ludus_win_privesc_sebackup: true
          ludus_win_privesc_unquoted_service_path: true
          ludus_win_privesc_weak_service_binary: true
          ludus_win_privesc_weak_service_registry: true
          ludus_win_privesc_hardcoded_credentials_dotnet: true
          ludus_win_privesc_stored_credentials_winlogon: true
          ludus_win_privesc_leaked_creds_ps_history: true
          ludus_win_privesc_leaked_creds_git: true
          ludus_win_privesc_answer_files: true
          ludus_win_privesc_always_install_elevated: true
          ludus_win_privesc_weak_startup_folder: true
          ludus_win_privesc_weak_registry_run_key: true
          ludus_win_privesc_uac_bypass_setup: false  # handled on WIN11-UAC VM

          # ── Group A: Token Privileges ─────────────────────────────────────────
          ludus_win_privesc_sedebug: true
          ludus_win_privesc_serestore: true
          ludus_win_privesc_setakeownership: true
          ludus_win_privesc_seloaddriver: true
          ludus_win_privesc_semanagevolume: true
          ludus_win_privesc_backup_operators_group: true
          ludus_win_privesc_server_operators_group: false  # Server Operators meaningless on Standalone 

          # ── Group B: Registry / Security Config ───────────────────────────────
          ludus_win_privesc_wdigest_enabled: true
          ludus_win_privesc_wsus_http: true
          ludus_win_privesc_applocker_disabled: true
          ludus_win_privesc_applocker_weak_rules: false

          # ── Group C: Path / File ──────────────────────────────────────────────
          ludus_win_privesc_dll_hijacking_path: true
          ludus_win_privesc_dll_hijack_dir: "C:\\DevUtils"
          ludus_win_privesc_gpp_cpassword: true
          ludus_win_privesc_wifi_profile: true
          ludus_win_privesc_ssh_private_keys: true
          ludus_win_privesc_office_creds: true

  # ── Windows 11 — UAC Bypass Scenario ────────────────────────────────────────
  # Dedicated VM for UAC bypass testing.
  # uacuser is in local Administrators but has NO extra token privileges —
  # goal is purely UAC bypass to reach High Integrity.
  # lowpriv on this VM has token privileges but is NOT in Administrators,
  # so whoami /priv shows all assigned privileges directly.
  - vm_name: "{{ range_id }}-WIN11-UAC"
    hostname: "WIN11UAC"
    template: win11-22h2-x64-enterprise-template
    vlan: 10
    ip_last_octet: 52
    ram_gb: 4
    cpus: 2
    windows:
      sysprep: true
    roles:
      - name: ludus_win_privesc_new
        vars:
          # ── User config ──────────────────────────────────────────────────────
          ludus_win_privesc_victim_user_type: "local"
          ludus_win_privesc_victim_user: "lowpriv"
          ludus_win_privesc_victim_password: "Password123!"
          ludus_win_privesc_admin_user: "Administrator"
          ludus_win_privesc_admin_password: "P@ssw0rd123!"

          # ── Defender ─────────────────────────────────────────────────────────
          ludus_win_privesc_install_defender_gui: false
          ludus_win_privesc_disable_defender: true
          ludus_win_privesc_reboot_after_privs: true

          # ── UAC Bypass scenario ───────────────────────────────────────────────
          # lowpriv is NOT in Administrators → whoami /priv shows all privs directly
          # uacuser IS in Administrators → must bypass UAC to reach High Integrity
          ludus_win_privesc_uac_bypass_setup: true   # adds uacuser to Administrators

          # ── Token Privileges (all enabled — visible without elevation) ────────
          # lowpriv is a plain user → no UAC filtering → all privs visible in token
          ludus_win_privesc_sedebug: true
          ludus_win_privesc_sebackup: true
          ludus_win_privesc_serestore: true
          ludus_win_privesc_setakeownership: true
          ludus_win_privesc_seloaddriver: true
          ludus_win_privesc_semanagevolume: true
          ludus_win_privesc_seimpersonate: true
          ludus_win_privesc_backup_operators_group: true
          ludus_win_privesc_server_operators_group: false

          # ── All other scenarios disabled on this VM ───────────────────────────
          ludus_win_privesc_wdigest_enabled: false
          ludus_win_privesc_wsus_http: false
          ludus_win_privesc_applocker_disabled: false
          ludus_win_privesc_dll_hijacking_path: false
          ludus_win_privesc_gpp_cpassword: false
          ludus_win_privesc_wifi_profile: false
          ludus_win_privesc_ssh_private_keys: false
          ludus_win_privesc_office_creds: false
          ludus_win_privesc_unquoted_service_path: false
          ludus_win_privesc_weak_service_binary: false
          ludus_win_privesc_weak_service_registry: false
          ludus_win_privesc_hardcoded_credentials_dotnet: false
          ludus_win_privesc_stored_credentials_winlogon: false
          ludus_win_privesc_leaked_creds_ps_history: false
          ludus_win_privesc_leaked_creds_git: false
          ludus_win_privesc_answer_files: false
          ludus_win_privesc_always_install_elevated: false
          ludus_win_privesc_weak_startup_folder: false
          ludus_win_privesc_weak_registry_run_key: false
```

---

## Example Ludus Range Config — Domain-joined (domain user, automatic user creation) -> not tested 

Deploys a DC plus two domain-joined member VMs. Uses the Ludus built-in
`disable_defender` GPO — no manual Tamper Protection step required.
The role automatically creates the `lowpriv` victim user in Active Directory
when running on the DC (`ludus_win_privesc_create_domain_user: true`).

```yaml
---

ludus:

  # ── Kali Attack Box ──────────────────────────────────────────────────────────
  - vm_name: "{{ range_id }}-KALI"
    hostname: "{{ range_id }}-kali"
    template: kali-x64-desktop-template
    vlan: 10
    ip_last_octet: 99
    ram_gb: 4
    cpus: 2
    linux: true

  # ── Domain Controller ────────────────────────────────────────────────────────
  # The role also runs on the DC — but only the create_domain_user task
  # executes there (when: domain_controller role). All other scenarios run
  # on member VMs only.
  - vm_name: "{{ range_id }}-DC01"
    hostname: "DC01"
    template: win2022-server-x64-template
    vlan: 10
    ip_last_octet: 10
    ram_gb: 4
    cpus: 2
    windows:
      sysprep: true
    domain:
      fqdn: lab.local
      role: primary-dc
    roles:
      - name: ludus_win_privesc_new
        vars:
          ludus_win_privesc_victim_user_type: "domain"
          ludus_win_privesc_victim_user: "lowpriv"
          ludus_win_privesc_victim_password: "Password123!"
          ludus_win_privesc_domain: "LAB"
          ludus_win_privesc_domain_fqdn: "lab.local"
          ludus_win_privesc_create_domain_user: true
          ludus_win_privesc_victim_ou: ""           # default Users container
          # Alle Szenarien deaktiviert auf dem DC — nur User-Erstellung läuft
          ludus_win_privesc_disable_defender: false
          ludus_win_privesc_install_defender_gui: false
          ludus_win_privesc_reboot_after_privs: true  # false = 1 Reboot weniger
          ludus_win_privesc_seimpersonate: false
          ludus_win_privesc_sebackup: false
          ludus_win_privesc_unquoted_service_path: false
          ludus_win_privesc_weak_service_binary: false
          ludus_win_privesc_weak_service_registry: false
          ludus_win_privesc_hardcoded_credentials_dotnet: false
          ludus_win_privesc_stored_credentials_winlogon: false
          ludus_win_privesc_leaked_creds_ps_history: false
          ludus_win_privesc_leaked_creds_git: false
          ludus_win_privesc_answer_files: false
          ludus_win_privesc_always_install_elevated: false
          ludus_win_privesc_weak_startup_folder: false
          ludus_win_privesc_weak_registry_run_key: false
          ludus_win_privesc_uac_bypass_setup: false
          ludus_win_privesc_sedebug: false
          ludus_win_privesc_serestore: false
          ludus_win_privesc_setakeownership: false
          ludus_win_privesc_seloaddriver: false
          ludus_win_privesc_semanagevolume: false
          ludus_win_privesc_backup_operators_group: false
          ludus_win_privesc_server_operators_group: false
          ludus_win_privesc_wdigest_enabled: false
          ludus_win_privesc_wsus_http: false
          ludus_win_privesc_applocker_disabled: false
          ludus_win_privesc_dll_hijacking_path: false
          ludus_win_privesc_gpp_cpassword: false
          ludus_win_privesc_wifi_profile: false
          ludus_win_privesc_ssh_private_keys: false
          ludus_win_privesc_office_creds: false

  # ── Windows Server 2022 — Domain-Joined ──────────────────────────────────────
  # Ludus disable_defender GPO deaktiviert Defender inkl. Tamper Protection
  # via domain policy — no manual Tamper Protection step needed.
  - vm_name: "{{ range_id }}-WIN2022"
    hostname: "WIN2022"
    template: win2022-server-x64-template
    vlan: 10
    ip_last_octet: 50
    ram_gb: 4
    cpus: 2
    windows:
      sysprep: true
    domain:
      fqdn: lab.local
      role: member
    gpos:
      - disable_defender     # Ludus built-in: erstellt und verknüpft GPO die
                             # Defender für alle Domain-Windows-Maschinen deaktiviert
    roles:
      - name: ludus_win_privesc_new
        vars:
          # ── User config ──────────────────────────────────────────────────────
          # Domain-User Modus: lowpriv muss in AD existieren (von DC erstellt)
          ludus_win_privesc_victim_user_type: "domain"
          ludus_win_privesc_victim_user: "lowpriv"
          ludus_win_privesc_domain: "LAB"
          ludus_win_privesc_domain_fqdn: "lab.local"
          ludus_win_privesc_create_domain_user: false   # DC-Role erstellt den User
          ludus_win_privesc_admin_user: "Administrator"
          ludus_win_privesc_admin_password: "P@ssw0rd123!"

          # ── Defender ─────────────────────────────────────────────────────────
          # GPO übernimmt Defender-Deaktivierung → disable_defender: false
          # No Tamper Protection issue — GPO handles Defender.
          ludus_win_privesc_disable_defender: false
          ludus_win_privesc_install_defender_gui: false
          ludus_win_privesc_reboot_after_privs: true  # false = 1 Reboot weniger

          # ── Original Scenarios ────────────────────────────────────────────────
          ludus_win_privesc_seimpersonate: true     
          ludus_win_privesc_sebackup: true
          ludus_win_privesc_unquoted_service_path: true
          ludus_win_privesc_weak_service_binary: true
          ludus_win_privesc_weak_service_registry: true
          ludus_win_privesc_hardcoded_credentials_dotnet: true
          ludus_win_privesc_stored_credentials_winlogon: true
          ludus_win_privesc_leaked_creds_ps_history: true
          ludus_win_privesc_leaked_creds_git: true
          ludus_win_privesc_answer_files: true
          ludus_win_privesc_always_install_elevated: true
          ludus_win_privesc_weak_startup_folder: true
          ludus_win_privesc_weak_registry_run_key: true
          ludus_win_privesc_uac_bypass_setup: true

          # ── Group A: Token Privileges ─────────────────────────────────────────
          ludus_win_privesc_sedebug: true
          ludus_win_privesc_serestore: true
          ludus_win_privesc_setakeownership: true
          ludus_win_privesc_seloaddriver: true
          ludus_win_privesc_semanagevolume: true
          ludus_win_privesc_backup_operators_group: true
          ludus_win_privesc_server_operators_group: true

          # ── Group B: Registry / Security Config ───────────────────────────────
          ludus_win_privesc_wdigest_enabled: true
          ludus_win_privesc_wsus_http: true
          ludus_win_privesc_applocker_disabled: true
          ludus_win_privesc_applocker_weak_rules: false

          # ── Group C: Path / File ──────────────────────────────────────────────
          ludus_win_privesc_dll_hijacking_path: true
          ludus_win_privesc_dll_hijack_dir: "C:\\DevUtils"
          ludus_win_privesc_gpp_cpassword: true
          ludus_win_privesc_wifi_profile: true
          ludus_win_privesc_ssh_private_keys: true
          ludus_win_privesc_office_creds: true

  # ── Windows 11 — Domain-Joined ───────────────────────────────────────────────
  - vm_name: "{{ range_id }}-WIN11"
    hostname: "WIN11"
    template: win11-22h2-x64-enterprise-template
    vlan: 10
    ip_last_octet: 51
    ram_gb: 4
    cpus: 2
    windows:
      sysprep: true
    domain:
      fqdn: lab.local
      role: member
    gpos:
      - disable_defender     # also applies to Win11 — no manual TP step needed
    roles:
      - name: ludus_win_privesc_new
        vars:
          # ── User config ──────────────────────────────────────────────────────
          ludus_win_privesc_victim_user_type: "domain"
          ludus_win_privesc_victim_user: "lowpriv"
          ludus_win_privesc_domain: "LAB"
          ludus_win_privesc_domain_fqdn: "lab.local"
          ludus_win_privesc_create_domain_user: false   # DC-Role erstellt den User
          ludus_win_privesc_admin_user: "Administrator"
          ludus_win_privesc_admin_password: "P@ssw0rd123!"

          # ── Defender ─────────────────────────────────────────────────────────
          # GPO disable_defender applies automatically — no manual TP step needed
          ludus_win_privesc_disable_defender: false
          ludus_win_privesc_install_defender_gui: false
          ludus_win_privesc_reboot_after_privs: true  # false = 1 Reboot weniger

          # ── Original Scenarios ────────────────────────────────────────────────
          ludus_win_privesc_seimpersonate: true
          ludus_win_privesc_sebackup: true
          ludus_win_privesc_unquoted_service_path: true
          ludus_win_privesc_weak_service_binary: true
          ludus_win_privesc_weak_service_registry: true
          ludus_win_privesc_hardcoded_credentials_dotnet: true
          ludus_win_privesc_stored_credentials_winlogon: true
          ludus_win_privesc_leaked_creds_ps_history: true
          ludus_win_privesc_leaked_creds_git: true
          ludus_win_privesc_answer_files: true
          ludus_win_privesc_always_install_elevated: true
          ludus_win_privesc_weak_startup_folder: true
          ludus_win_privesc_weak_registry_run_key: true
          ludus_win_privesc_uac_bypass_setup: true

          # ── Group A: Token Privileges ─────────────────────────────────────────
          ludus_win_privesc_sedebug: true
          ludus_win_privesc_serestore: true
          ludus_win_privesc_setakeownership: true
          ludus_win_privesc_seloaddriver: true
          ludus_win_privesc_semanagevolume: true
          ludus_win_privesc_backup_operators_group: true
          ludus_win_privesc_server_operators_group: true 

          # ── Group B: Registry / Security Config ───────────────────────────────
          ludus_win_privesc_wdigest_enabled: true
          ludus_win_privesc_wsus_http: true
          ludus_win_privesc_applocker_disabled: true
          ludus_win_privesc_applocker_weak_rules: false

          # ── Group C: Path / File ──────────────────────────────────────────────
          ludus_win_privesc_dll_hijacking_path: true
          ludus_win_privesc_dll_hijack_dir: "C:\\DevUtils"
          ludus_win_privesc_gpp_cpassword: true
          ludus_win_privesc_wifi_profile: true
          ludus_win_privesc_ssh_private_keys: true
          ludus_win_privesc_office_creds: true
```

---

## Installation

```bash
ludus ansible roles add mojeda101.ludus_win_privesc
```

Or from local directory:

```bash
ludus ansible roles add -d ./ludus_win_privesc/
```

Deploy:

```bash
ludus range config set -f ludus-range-config.yml
ludus range deploy
```


---

## Credits

- Vulnerability scenarios: [nickvourd/Windows-Local-Privilege-Escalation-Cookbook](https://github.com/nickvourd/Windows-Local-Privilege-Escalation-Cookbook)

## License

MIT

## Author Information

This role was created by [mojeda101](https://github.com/mojeda101) for [Ludus](https://ludus.cloud/).

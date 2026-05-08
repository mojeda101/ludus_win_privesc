# ludus_win_privesc

An Ansible role for [Ludus](https://ludus.cloud/) that deploys **32 Windows privilege escalation lab scenarios** on Windows 10/11 and Windows Server 2019/2022. Each scenario is individually toggle-able and idempotent.

Scenarios cover token privileges, service misconfigurations, registry weaknesses, credential discovery, and path/file vulnerabilities — providing a realistic environment for practicing local privilege escalation techniques.

Every credential-discovery scenario can also be wired to a different identity from your range, so each planted misconfiguration leaks a different account — see [Per-Scenario Credential Overrides](#per-scenario-credential-overrides) below.

---

## Scenarios Covered

All 32 scenarios are individually toggle-able via boolean variables:

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
| **Credential Discovery** | Leaked Credentials in Files (.env, .ps1, .py, .sql, .tfvars etc.) | `ludus_win_privesc_leaked_creds_files` |
| **Kernel / Service** | PrintNightmare (CVE-2021-34527) | `ludus_win_privesc_printnightmare` |

---

## ⚠️ Tamper Protection — What It Affects and What It Does Not

> **Important:** Tamper Protection (TP) only blocks writes to Windows Defender
> registry keys.

**Tamper Protection only needs to be disabled when `ludus_win_privesc_disable_defender: true`.**

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
- **Tamper Protection must be disabled before the first role run** only if  `disable_defender: true`

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

## Per-Scenario Credential Overrides

By default, every credential-bearing scenario uses the shared
`ludus_win_privesc_admin_user` / `ludus_win_privesc_admin_password` values.
That's fine for unit-testing the role, but unrealistic for a full lab where
you want different misconfigurations to leak different identities — e.g.
a Winlogon AutoLogon entry from a help-desk account, an Unattend.xml from
a deployment account, a developer's leaked git creds for a service account.

These overrides let you map each scenario to a distinct identity. **Every
override defaults to the shared admin credentials**, so set them only when
you want differentiation. Mixing them lets a discovered credential unlock
exactly one specific pivot, instead of opening every door at once.

### Per-scenario credential variables

| Variable | Default | Used by |
|---|---|---|
| `ludus_win_privesc_winlogon_user` / `_password` | shared admin | Winlogon AutoLogon registry |
| `ludus_win_privesc_ps_history_user` / `_password` | shared admin | PSReadLine history |
| `ludus_win_privesc_git_user` / `_password` | shared admin | Git config + .env in commit history |
| `ludus_win_privesc_git_email` | `dev@company.local` | git `user.email` |
| `ludus_win_privesc_git_dev_name` | `Developer` | git `user.name` |
| `ludus_win_privesc_files_user` / `_password` | shared admin | .env, .config, .json, .yml, .ini, .ps1, .py, .sql, .tfvars, .log, .txt files |
| `ludus_win_privesc_unattend_user` / `_password` | shared admin | Unattend.xml AutoLogon + LocalAccount blocks |

### Office documents — 15 separate identities

The `office_creds.yml` task drops several IT-team-style password notes
(Passwoerter.txt, IT-Server-Credentials.txt, passwords_export.csv,
IT-Notizen.txt, Passwort-Liste.csv, a Word doc, plus a randomly named
folder of config files). Each row in those documents now maps to its own
variable so you can wire each one to a real account in your range:

| Variable | Default | Appears in |
|---|---|---|
| `ludus_win_privesc_office_admin_user` / `_password` | shared admin | All documents (primary admin row) |
| `ludus_win_privesc_office_dc_user` / `_password` | `Administrator` / `DC@dmin2024!` | DC entry in IT notes / CSV |
| `ludus_win_privesc_office_sql_password` | `SqlAdmin2024!` | SQL Server SA row |
| `ludus_win_privesc_office_backup_user` / `_password` | `Backup-Account` / `B@ckup2023sicher` | Backup account row |
| `ludus_win_privesc_office_veeam_user` / `_password` | `veeam_svc` / `V33m@Backup!` | Veeam service row |
| `ludus_win_privesc_office_wsus_user` / `_password` | `svc_wsus` / `wsus_svc_P@ss!` | WSUS service row |
| `ludus_win_privesc_office_router_password` | `router@lab123` | Router admin row |
| `ludus_win_privesc_office_wifi_corp_ssid` / `_password` | `LabNetwork` / `LabWifiSecret99!` | WiFi SSID + password |
| `ludus_win_privesc_office_linux_user` / `_password` | `root` / `TuxSecure2024` | Linux host row |
| `ludus_win_privesc_office_breakglass_user` / `_password` | `bgadmin` / `Br3@kGl@ss!!` | Break-glass admin row in IT notes |
| `ludus_win_privesc_office_pwmanager_url` | `""` | (Optional) Password manager URL added to IT notes |
| `ludus_win_privesc_office_pwmanager_note` | `""` | (Optional) Free-text line added to IT notes |

### Internal infrastructure references

These hostnames and IPs are written into many of the planted artifacts —
PS history, git commits, config files, known_hosts, SSH config files, and
IT notes. Override them so the recon trail in your lab points at hosts the
attacker can actually scan and reach on the network.

| Variable | Default |
|---|---|
| `ludus_win_privesc_internal_domain_fqdn` | `lab.local` |
| `ludus_win_privesc_internal_dc_host` / `_dc_ip` | `dc01.lab.local` / `10.10.10.10` |
| `ludus_win_privesc_internal_fileserver_host` / `_ip` / `_share` | `fileserver.lab.local` / `10.10.10.20` / `share` |
| `ludus_win_privesc_internal_veeam_host` / `_ip` | `veeam.lab.local` / `10.10.10.40` |
| `ludus_win_privesc_internal_linux_host` / `_ip` | `linuxdev.lab.local` / `10.10.10.30` |
| `ludus_win_privesc_internal_db_host` / `_db_port` / `_db_name` | `db.lab.local` / `1433` / `AppDB` |
| `ludus_win_privesc_internal_mail_host` | `mail.lab.local` |
| `ludus_win_privesc_internal_jumpbox_ip` | `10.10.10.1` |
| `ludus_win_privesc_internal_win2022_ip` | `10.10.10.50` |

### Example: complete Ludus range configs

Two complete examples follow — pick the one that matches how you're using
the role. The standalone (local-mode) example is a single-VM lab that
showcases all the per-scenario credential overrides without needing AD;
the domain example shows the same identities mapped onto a real corp.local
tenant for connected-lab scenarios.

#### Example 1 — Standalone (local mode, single VM)

A single Windows 11 host, no domain. The role creates `lowpriv` and
`Administrator` locally; every credential-discovery scenario then references
a different *fictional* identity baked into the documents and registry.
There's nothing for the discovered creds to authenticate to (since this is
a one-VM lab) — the value is in practicing the discovery techniques and
seeing what realistic differentiated identity-leakage looks like across a
real workstation.

```yaml
ludus:
  - vm_name: "{{ range_id }}-privesc-ws"
    hostname: "PRIVESC-WS"
    template: win11-22h2-x64-enterprise-template
    vlan: 10
    ip_last_octet: 11
    ram_gb: 4
    cpus: 2
    windows:
      sysprep: true
    roles:
      - mojeda101.ludus_win_privesc
    role_vars:
      # ── Victim & shared admin (created by the role) ──────────────────────
      ludus_win_privesc_victim_user_type: "local"
      ludus_win_privesc_victim_user: "lowpriv"
      ludus_win_privesc_victim_password: "Password123!"
      ludus_win_privesc_admin_user: "Administrator"
      ludus_win_privesc_admin_password: "P@ssw0rd123!"

      # uac_bypass_setup off if you want a pure low-priv-domain-style story;
      # leave it on for full UAC bypass practice in standalone mode.
      ludus_win_privesc_uac_bypass_setup: true

      # ══════════════════════════════════════════════════════════════════════
      # Per-scenario identity mapping — each credential plant references a
      # different fictional account, so the documents read like a real
      # workstation with multiple distinct identities scattered across it.
      # ══════════════════════════════════════════════════════════════════════

      # Winlogon AutoLogon — IT helpdesk left themselves a backdoor.
      ludus_win_privesc_winlogon_user: "helpdesk-svc"
      ludus_win_privesc_winlogon_password: "HelpD3sk!2024"

      # PowerShell history — an LDAP bind script ran here recently.
      ludus_win_privesc_ps_history_user: "ldap-bind"
      ludus_win_privesc_ps_history_password: "Ldap@Bind#2024"

      # Git repo — developer committed a Postgres password.
      ludus_win_privesc_git_user: "postgres"
      ludus_win_privesc_git_password: "DB@Postgres2024!"
      ludus_win_privesc_git_dev_name: "Jane Doe"
      ludus_win_privesc_git_email: "j.doe@corp.local"

      # Config files — same Postgres credentials sprayed across .env etc.
      ludus_win_privesc_files_user: "postgres"
      ludus_win_privesc_files_password: "DB@Postgres2024!"

      # Unattend.xml — leftover from the workstation imaging account.
      ludus_win_privesc_unattend_user: "image-build"
      ludus_win_privesc_unattend_password: "Im@ge2024!"

      # Office docs — IT password notes referencing several accounts.
      ludus_win_privesc_office_admin_user: "helpdesk-svc"
      ludus_win_privesc_office_admin_password: "HelpD3sk!2024"
      ludus_win_privesc_office_dc_user: "domainadmin"
      ludus_win_privesc_office_dc_password: "DA-Real-Pw-2024!"
      ludus_win_privesc_office_veeam_user: "veeam-repo-user"
      ludus_win_privesc_office_veeam_password: "Veeam@Repo2024"
      ludus_win_privesc_office_linux_user: "veeam-repo-user"
      ludus_win_privesc_office_linux_password: "Veeam@Repo2024"

      # Internal infrastructure — fictional hostnames that show up in the
      # planted artifacts (known_hosts, SSH config, IT notes).
      ludus_win_privesc_internal_domain_fqdn: "corp.local"
      ludus_win_privesc_internal_dc_host: "dc01.corp.local"
      ludus_win_privesc_internal_fileserver_host: "fs01.corp.local"
      ludus_win_privesc_internal_veeam_host: "veeam.corp.local"
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
      ludus_win_privesc_internal_db_host: "db.corp.local"        # NEW
      ludus_win_privesc_internal_linux_host: "linuxdev.corp.local"  # NEW
      ludus_win_privesc_internal_mail_host: "mail.corp.local"    # NEW
network:
  inter_vlan_default: ACCEPT
```

What the trainee practices on this single box: enumerating Winlogon
registry, parsing PSReadLine history, walking git history with
`git log -p --all`, finding `.env` and `Unattend.xml` files, and reading
the planted Office docs. They'll discover ~8 distinct identities across
the various plants — the lesson being that one workstation can leak many
different accounts depending on where you look.

#### Example 2 — Domain-joined (corp.local AD, two VMs)

> **Status: untested.** The role's domain-mode wiring (auto-created victim
> users, `_create_domain_user`, etc.) is in place and the per-scenario
> overrides plug into it cleanly in principle, but this exact two-VM
> configuration has not been deployed end-to-end yet. If you try it, please
> open an issue with what worked and what didn't. Treat the standalone
> example above as the canonical "known-good" path until then.

A fully deployable two-VM range using `corp.local` as the fictional company.
The `corp-dc` builds the AD tenant with a handful of distinct identities;
the `corp-ws01` workstation runs this role in domain mode and wires every
credential-discovery scenario to a different account from that tenant.

The interesting property of this version: discovered credentials *actually
authenticate*. Recovering `helpdesk-svc` from Winlogon gives an attacker
real RDP rights to the workstation. Recovering `domainadmin` from the
office docs gives them the real DA password to the real DC. The lesson is
the same — chain across discoveries to escalate — but with a working
authentication target on the other end.

##### Domain User Creation -> untested 

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

```yaml
ludus:
  # ──────────────────────────────────────────────────────────────────────────
  # Domain Controller — creates the identities the privesc role will reference
  # ──────────────────────────────────────────────────────────────────────────
  - vm_name: "{{ range_id }}-corp-dc"
    hostname: "DC01"
    template: win2022-server-x64-template
    vlan: 10
    ip_last_octet: 10
    ram_gb: 4
    cpus: 2
    windows:
      sysprep: true
    domain:
      fqdn: corp.local
      role: primary-dc
    roles:
      - ludus_ad_group_all
    role_vars:
      ludus_ad:
        users:
          # Domain Admin — the prize. Should ONLY be discoverable via the
          # office_dc_* office_creds entry.
          - name: domainadmin
            firstname: Domain
            surname: Admin
            display_name: Domain Admin
            password: "DA-Real-Pw-2024!"
            path: "DC=corp,DC=local"
            groups: [Domain Users, Domain Admins]

          # Help desk service account — reachable via the Winlogon AutoLogon
          # registry plant. Limited blast radius (no DA, no remote admin).
          - name: helpdesk-svc
            firstname: Helpdesk
            surname: Service
            display_name: Helpdesk Service
            password: "HelpD3sk!2024"
            path: "DC=corp,DC=local"
            groups: [Domain Users]

          # LDAP bind account — appears in PowerShell history. Read-only on
          # AD; useful for enumeration but not direct compromise.
          - name: ldap-bind
            firstname: LDAP
            surname: Bind
            display_name: LDAP Bind
            password: "Ldap@Bind#2024"
            path: "DC=corp,DC=local"
            groups: [Domain Users]

          # Image-build account — appears in Unattend.xml. Local admin on
          # workstations during deployment, no domain rights.
          - name: image-build
            firstname: Image
            surname: Build
            display_name: Image Build
            password: "Im@ge2024!"
            path: "DC=corp,DC=local"
            groups: [Domain Users]

          # Veeam repo account — appears in the Office docs. Gives SSH to a
          # backup repo (which would be a separate VM in your real lab).
          - name: veeam-repo-user
            firstname: Veeam
            surname: Repo
            display_name: Veeam Repo
            password: "Veeam@Repo2024"
            path: "DC=corp,DC=local"
            groups: [Domain Users]

          # The victim user — what the foothold lands as.
          - name: lowpriv
            firstname: Low
            surname: Priv
            display_name: Low Priv
            password: "Password123!"
            path: "DC=corp,DC=local"
            groups: [Domain Users]

  # ──────────────────────────────────────────────────────────────────────────
  # Workstation — runs ludus_win_privesc with per-scenario identity mapping
  # ──────────────────────────────────────────────────────────────────────────
  - vm_name: "{{ range_id }}-corp-ws01"
    hostname: "WS01"
    template: win11-22h2-x64-enterprise-template
    vlan: 10
    ip_last_octet: 11
    ram_gb: 4
    cpus: 2
    windows:
      sysprep: true
    domain:
      fqdn: corp.local
      role: member
    roles:
      - name: mojeda101.ludus_win_privesc
        depends_on:
          - vm_name: "{{ range_id }}-corp-dc"
            role: ludus_ad_group_all
    role_vars:
      # ── Victim & shared admin ────────────────────────────────────────────
      ludus_win_privesc_victim_user_type: "domain"
      ludus_win_privesc_victim_user: "lowpriv"
      ludus_win_privesc_create_domain_user: false  # already created above
      ludus_win_privesc_admin_user: "Administrator"
      ludus_win_privesc_admin_password: "P@ssw0rd123!"

      # uac_bypass_setup off because it requires the victim to be local admin,
      # which contradicts the "domain low-priv user" foothold.
      ludus_win_privesc_uac_bypass_setup: false

      # ══════════════════════════════════════════════════════════════════════
      # Per-scenario identity mapping (same as the standalone example, but
      # these identities now correspond to REAL accounts in corp.local AD)
      # ══════════════════════════════════════════════════════════════════════

      ludus_win_privesc_winlogon_user: "helpdesk-svc"
      ludus_win_privesc_winlogon_password: "HelpD3sk!2024"

      ludus_win_privesc_ps_history_user: "ldap-bind"
      ludus_win_privesc_ps_history_password: "Ldap@Bind#2024"

      ludus_win_privesc_git_user: "postgres"
      ludus_win_privesc_git_password: "DB@Postgres2024!"
      ludus_win_privesc_git_dev_name: "Jane Doe"
      ludus_win_privesc_git_email: "j.doe@corp.local"

      ludus_win_privesc_files_user: "postgres"
      ludus_win_privesc_files_password: "DB@Postgres2024!"

      ludus_win_privesc_unattend_user: "image-build"
      ludus_win_privesc_unattend_password: "Im@ge2024!"

      ludus_win_privesc_office_admin_user: "helpdesk-svc"
      ludus_win_privesc_office_admin_password: "HelpD3sk!2024"
      ludus_win_privesc_office_dc_user: "domainadmin"
      ludus_win_privesc_office_dc_password: "DA-Real-Pw-2024!"
      ludus_win_privesc_office_veeam_user: "veeam-repo-user"
      ludus_win_privesc_office_veeam_password: "Veeam@Repo2024"
      ludus_win_privesc_office_linux_user: "veeam-repo-user"
      ludus_win_privesc_office_linux_password: "Veeam@Repo2024"

      # Internal infrastructure — point references at real hosts in this
      # range so known_hosts, ssh config, and IT notes line up with what
      # the attacker can actually scan on the network.
      ludus_win_privesc_internal_domain_fqdn: "corp.local"
      ludus_win_privesc_internal_dc_host: "dc01.corp.local"
      ludus_win_privesc_internal_dc_ip: "10.2.10.10"   # adjust to your range_id
      ludus_win_privesc_internal_fileserver_host: "fs01.corp.local"
      ludus_win_privesc_internal_fileserver_ip: "10.2.10.20"

network:
  inter_vlan_default: ACCEPT
```

With this configuration, an attacker who recovers the Winlogon password
gets `helpdesk-svc` — a limited account that can RDP to workstations but
not to servers or the DC. The git history reveals `postgres`, useful for
hitting databases but nothing else. To reach Domain Admin they have to
*chain* across discoveries — e.g. use the helpdesk account to RDP into
this workstation, find the office docs in `C:\IT\`, and only then read
the DA password out of `IT-Server-Credentials.txt`.

The `ludus_win_privesc_internal_*_ip` values use `10.2.10.x` because Ludus
does not expand `{{ range_id }}` inside `role_vars` strings — adjust the
second octet to your actual range_id (e.g. `10.42.10.10` for range 42).

```yaml
ludus:
  # ──────────────────────────────────────────────────────────────────────────
  # Domain Controller — creates the identities the privesc role will reference
  # ──────────────────────────────────────────────────────────────────────────
  - vm_name: "{{ range_id }}-corp-dc"
    hostname: "DC01"
    template: win2022-server-x64-template
    vlan: 10
    ip_last_octet: 10
    ram_gb: 4
    cpus: 2
    windows:
      sysprep: true
    domain:
      fqdn: corp.local
      role: primary-dc
    roles:
      - ludus_ad_group_all
    role_vars:
      ludus_ad:
        users:
          # Domain Admin — the prize. Should ONLY be discoverable via the
          # office_dc_* office_creds entry.
          - name: domainadmin
            firstname: Domain
            surname: Admin
            display_name: Domain Admin
            password: "DA-Real-Pw-2024!"
            path: "DC=corp,DC=local"
            groups: [Domain Users, Domain Admins]

          # Help desk service account — reachable via the Winlogon AutoLogon
          # registry plant. Limited blast radius (no DA, no remote admin).
          - name: helpdesk-svc
            firstname: Helpdesk
            surname: Service
            display_name: Helpdesk Service
            password: "HelpD3sk!2024"
            path: "DC=corp,DC=local"
            groups: [Domain Users]

          # LDAP bind account — appears in PowerShell history. Read-only on
          # AD; useful for enumeration but not direct compromise.
          - name: ldap-bind
            firstname: LDAP
            surname: Bind
            display_name: LDAP Bind
            password: "Ldap@Bind#2024"
            path: "DC=corp,DC=local"
            groups: [Domain Users]

          # Image-build account — appears in Unattend.xml. Local admin on
          # workstations during deployment, no domain rights.
          - name: image-build
            firstname: Image
            surname: Build
            display_name: Image Build
            password: "Im@ge2024!"
            path: "DC=corp,DC=local"
            groups: [Domain Users]

          # Veeam repo account — appears in the Office docs. Gives SSH to a
          # backup repo (which would be a separate VM in your real lab).
          - name: veeam-repo-user
            firstname: Veeam
            surname: Repo
            display_name: Veeam Repo
            password: "Veeam@Repo2024"
            path: "DC=corp,DC=local"
            groups: [Domain Users]

          # The victim user — what the foothold lands as.
          - name: lowpriv
            firstname: Low
            surname: Priv
            display_name: Low Priv
            password: "Password123!"
            path: "DC=corp,DC=local"
            groups: [Domain Users]

  # ──────────────────────────────────────────────────────────────────────────
  # Workstation — runs ludus_win_privesc with per-scenario identity mapping
  # ──────────────────────────────────────────────────────────────────────────
  - vm_name: "{{ range_id }}-corp-ws01"
    hostname: "WS01"
    template: win11-22h2-x64-enterprise-template
    vlan: 10
    ip_last_octet: 11
    ram_gb: 4
    cpus: 2
    windows:
      sysprep: true
    domain:
      fqdn: corp.local
      role: member
    roles:
      - name: mojeda101.ludus_win_privesc
        depends_on:
          - vm_name: "{{ range_id }}-corp-dc"
            role: ludus_ad_group_all
    role_vars:
      # ── Victim & shared admin ────────────────────────────────────────────
      ludus_win_privesc_victim_user_type: "domain"
      ludus_win_privesc_victim_user: "lowpriv"
      ludus_win_privesc_create_domain_user: false  # already created above
      ludus_win_privesc_admin_user: "Administrator"
      ludus_win_privesc_admin_password: "P@ssw0rd123!"

      # ── All scenarios on (defaults) ──────────────────────────────────────
      # uac_bypass_setup off because it requires the victim to be local admin,
      # which contradicts the "domain low-priv user" foothold.
      ludus_win_privesc_uac_bypass_setup: false

      # ══════════════════════════════════════════════════════════════════════
      # Per-scenario identity mapping: each credential-discovery scenario
      # leaks a DIFFERENT real account from corp.local.
      # ══════════════════════════════════════════════════════════════════════

      # Winlogon AutoLogon — IT left the helpdesk-svc password in the registry
      # so they could remote in for break/fix. Discovered via:
      #   reg query "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"
      ludus_win_privesc_winlogon_user: "helpdesk-svc"
      ludus_win_privesc_winlogon_password: "HelpD3sk!2024"

      # PowerShell history — a previous session ran a script that bound to
      # AD with the ldap-bind account. Discovered via:
      #   type $env:APPDATA\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt
      ludus_win_privesc_ps_history_user: "ldap-bind"
      ludus_win_privesc_ps_history_password: "Ldap@Bind#2024"

      # Git repo — a developer committed a Postgres password, then "removed"
      # it in a follow-up commit. Discovered via:
      #   cd C:\DevProjects\WebApp; git log -p --all | findstr /i password
      ludus_win_privesc_git_user: "postgres"
      ludus_win_privesc_git_password: "DB@Postgres2024!"
      ludus_win_privesc_git_dev_name: "Jane Doe"
      ludus_win_privesc_git_email: "j.doe@corp.local"

      # Config files (.env, .config, .json, .ini, .ps1, etc.) — same
      # postgres credentials sprayed across many file types.
      ludus_win_privesc_files_user: "postgres"
      ludus_win_privesc_files_password: "DB@Postgres2024!"

      # Unattend.xml — leftover from imaging. Local admin only, no domain
      # privileges, but useful for lateral movement to other workstations
      # built from the same image.
      ludus_win_privesc_unattend_user: "image-build"
      ludus_win_privesc_unattend_password: "Im@ge2024!"

      # Office docs — IT team password notes. The DC row leaks the actual
      # Domain Admin; the Veeam row leaks the backup repo SSH user.
      ludus_win_privesc_office_admin_user: "helpdesk-svc"
      ludus_win_privesc_office_admin_password: "HelpD3sk!2024"
      ludus_win_privesc_office_dc_user: "domainadmin"
      ludus_win_privesc_office_dc_password: "DA-Real-Pw-2024!"
      ludus_win_privesc_office_veeam_user: "veeam-repo-user"
      ludus_win_privesc_office_veeam_password: "Veeam@Repo2024"
      ludus_win_privesc_office_linux_user: "veeam-repo-user"
      ludus_win_privesc_office_linux_password: "Veeam@Repo2024"

      # Internal infrastructure — point references at real hosts in this
      # range so known_hosts, ssh config, and IT notes line up with what
      # the attacker can actually scan on the network.
      ludus_win_privesc_internal_domain_fqdn: "corp.local"
      ludus_win_privesc_internal_dc_host: "dc01.corp.local"
      ludus_win_privesc_internal_dc_ip: "10.2.10.10"   # adjust to your range_id
      ludus_win_privesc_internal_fileserver_host: "fs01.corp.local"
      ludus_win_privesc_internal_fileserver_ip: "10.2.10.20"

network:
  inter_vlan_default: ACCEPT
```

With this configuration, an attacker who recovers the Winlogon password
gets `helpdesk-svc` — a limited account that can RDP to workstations but
not to servers or the DC. The git history reveals `postgres`, useful for
hitting databases but nothing else. To reach Domain Admin they have to
*chain* across discoveries — e.g. use the helpdesk account to RDP into
this workstation, find the office docs in `C:\IT\`, and only then read
the DA password out of `IT-Server-Credentials.txt`.

The `ludus_win_privesc_internal_*_ip` values use `10.2.10.x` because Ludus
does not expand `{{ range_id }}` inside `role_vars` strings — adjust the
second octet to your actual range_id (e.g. `10.42.10.10` for range 42).

### Special characters in credential values

The role embeds your `_office_*` / `_admin_*` / `_internal_*` values into
PowerShell scripts that are sent through Ansible's `win_shell`. A handful
of characters have special meaning in either layer and can cause silent
corruption or hard deploy failures when they appear in passwords:

| Character | Symptom | Notes |
|---|---|---|
| `$` followed by a letter | Silent truncation. `Sql$Admin2024!` becomes `Sql!` in some files (PowerShell expands `$Admin2024` as a variable reference and finds nothing). | Hits `deploy.ps1` written by the random-folder block. The `.docx`, `.txt`, `.csv` artifacts written via `win_copy: content:` are unaffected. |
| `'` (single quote) | Ansible "failed at splitting arguments" error at deploy time, or PowerShell parse errors in the rendered shell script. | Always avoid in passwords. |
| `"` (double quote) | Breaks PS double-quoted strings the same way `'` breaks single-quoted ones. | Always avoid. |
| `` ` `` (backtick) | PS escape character — eaten silently or escapes the next character. | Always avoid. |

**Recommended safe character set for passwords**: alphanumerics plus
`! @ # % ^ & * ( ) - _ + = . , ; : ?`. Avoid `$ ' " \``.

This isn't great — a real ransomware lab benefits from realistic-looking
passwords, and `$` is common in real passwords. The role accepts the
constraint as a known limitation rather than introducing complex escape
machinery that creates its own deploy-time fragility. If you really need
`$`-bearing passwords in the planted artifacts, the `win_copy: content:`
artifacts (Passwoerter.txt, IT-Server-Credentials.txt, the CSVs) handle
them correctly — only the `deploy.ps1` and Winlogon-registry plants are
affected by the truncation.

### Scenarios that cannot be customized

A few scenarios have credentials baked in and cannot be overridden:

- **`hardcoded_creds_dotnet`** — the `CustomDotNetApp.exe` is downloaded
  pre-compiled from the cookbook repo. The strings inside are fixed.
  Discovered creds are good for practicing the *technique* (dnSpy,
  `strings`) but won't authenticate to anything else in your lab.
- **`gpp_cpassword`** — the `cpassword` value uses the publicly known
  AES key from MS14-025 and decrypts to `Password123!` by definition.
  Changing it breaks the decryption demo.

These limitations are noted in the relevant task files.

---

## Dependencies

None.

---


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

# Ansible Role: Windows Local Privilege Escalation Lab ([Ludus](https://ludus.cloud))

An Ansible Role that configures a Windows host with intentional privilege escalation misconfigurations for hands-on security training, based on the [Windows-Local-Privilege-Escalation-Cookbook](https://github.com/nickvourd/Windows-Local-Privilege-Escalation-Cookbook) by [@nickvourd](https://github.com/nickvourd).

> [!WARNING]
> This role intentionally introduces serious security vulnerabilities. **Only deploy in isolated lab environments.** Never apply to production systems.

## Scenarios Covered

All 15 cookbook scenarios are supported and individually toggle-able:

| Category | Scenario | Variable |
|---|---|---|
| **Privilege Tokens** | SeImpersonatePrivilege (+ IIS web shell) | `ludus_win_privesc_seimpersonate` |
| **Privilege Tokens** | SeBackupPrivilege | `ludus_win_privesc_sebackup` |
| **Service Misconfigs** | Unquoted Service Path | `ludus_win_privesc_unquoted_service_path` |
| **Service Misconfigs** | Weak Service Binary Permissions | `ludus_win_privesc_weak_service_binary` |
| **Service Misconfigs** | Weak Service Registry Permissions | `ludus_win_privesc_weak_service_registry` |
| **Credential Discovery** | Hardcoded Credentials (.NET App) | `ludus_win_privesc_hardcoded_credentials_dotnet` |
| **Credential Discovery** | Stored Credentials (Winlogon AutoLogon) | `ludus_win_privesc_stored_credentials_winlogon` |
| **Credential Discovery** | Leaked Credentials (PowerShell History) | `ludus_win_privesc_leaked_creds_ps_history` |
| **Credential Discovery** | Leaked Credentials (Git Repository) | `ludus_win_privesc_leaked_creds_git` |
| **Credential Discovery** | Answer Files (Unattend.xml) | `ludus_win_privesc_answer_files` |
| **Autostart Misconfigs** | AlwaysInstallElevated | `ludus_win_privesc_always_install_elevated` |
| **Autostart Misconfigs** | Weak Startup Folder Permissions | `ludus_win_privesc_weak_startup_folder` |
| **Autostart Misconfigs** | Weak Registry Run Key Permissions | `ludus_win_privesc_weak_registry_run_key` |
| **UAC** | UAC Bypass Lab Setup | `ludus_win_privesc_uac_bypass_setup` |

## Requirements

- Target must be a Windows host (Server 2019/2022 or Windows 10/11)
- WinRM must be configured (Ludus handles this automatically)
- Chocolatey must be installed on the target (required for the git credential leak scenario)
- For domain user mode: the host must be joined to an Active Directory domain

## Role Variables

Available variables with their defaults (see `defaults/main.yml`):

```yaml
# Disable Windows Defender (required for most scenarios to work cleanly) -> better diable with ludus gpo conf 
ludus_win_privesc_disable_defender: true

# Victim low-privilege user
# Set ludus_win_privesc_victim_user_type to 'local' or 'domain'
# For 'domain': the user must already exist in Active Directory - it will NOT be created
ludus_win_privesc_victim_user_type: "local"
ludus_win_privesc_victim_user: "lowpriv"
ludus_win_privesc_victim_password: "Password123!"  # only used when victim_user_type is 'local'

# Domain settings (only needed when victim_user_type is 'domain')
# ludus_win_privesc_domain is auto-detected from the Ludus range config if not set
ludus_win_privesc_domain: "CORP"

# Privileged account used in credential-leak scenarios
# (Winlogon stored creds, PS history, git leaks, answer files)
ludus_win_privesc_admin_user: "Administrator"
ludus_win_privesc_admin_password: "P@ssw0rd123!"

# Toggle each scenario (all enabled by default)
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
```

## Dependencies

None.

## Example Ludus Range Config — Standalone (local user)

```yaml
ludus:
  - vm_name: "{{ range_id }}-win-privesc-win11"
    hostname: "{{ range_id }}-PRIVESC"
    template: win11-22h2-x64-enterprise-template
    vlan: 10
    ip_last_octet: 50
    ram_gb: 4
    cpus: 2
    windows:
      sysprep: true
    roles:
      - mojeda101.ludus_win_privesc
    role_vars:
      ludus_win_privesc_victim_user_type: "local"
      ludus_win_privesc_victim_user: "student"
      ludus_win_privesc_victim_password: "TrainingPass1!"
      ludus_win_privesc_admin_user: "Administrator"
      ludus_win_privesc_admin_password: "AdminSecret99!"
```

## Example Ludus Range Config — Domain-joined (domain user)

> **Note:** The domain victim user must already exist in Active Directory before running this role. Create it via the DC or another role first.

```yaml
ludus:
  - vm_name: "{{ range_id }}-dc-win2022"
    hostname: "{{ range_id }}-DC01"
    template: win2022-server-x64-template
    vlan: 10
    ip_last_octet: 10
    ram_gb: 6
    cpus: 2
    windows:
      sysprep: true
    domain:
      fqdn: corp.local
      role: primary-dc

  - vm_name: "{{ range_id }}-win-privesc-win11"
    hostname: "{{ range_id }}-PRIVESC"
    template: win11-22h2-x64-enterprise-template
    vlan: 10
    ip_last_octet: 50
    ram_gb: 4
    cpus: 2
    windows:
      sysprep: true
    domain:
      fqdn: corp.local
      role: member
    roles:
      - mojeda101.ludus_win_privesc
    role_vars:
      ludus_win_privesc_victim_user_type: "domain"
      ludus_win_privesc_victim_user: "student"   # must already exist in AD
      ludus_win_privesc_admin_user: "Administrator"
      ludus_win_privesc_admin_password: "AdminSecret99!"
      # UAC bypass requires the victim to be a local admin - disable for pure domain user
      ludus_win_privesc_uac_bypass_setup: false
```

## Installation

```bash
ludus ansible roles add mojeda101.ludus_win_privesc
```

## Credits

- Vulnerability scenarios: [nickvourd/Windows-Local-Privilege-Escalation-Cookbook](https://github.com/nickvourd/Windows-Local-Privilege-Escalation-Cookbook)

## License

MIT

## Author Information

This role was created by [mojeda101](https://github.com/mojeda101), for [Ludus](https://ludus.cloud/).

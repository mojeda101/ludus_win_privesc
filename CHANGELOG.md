# Changelog

All notable changes to this role are documented here.

## [Unreleased]

### Added — Per-Scenario Credential Overrides

Every credential-bearing scenario can now reference a different identity
instead of all sharing the role's `admin_user` / `admin_password`. The
default behavior is unchanged: if you don't set any of the new variables,
each scenario falls back to the shared admin credentials exactly as before.

The motivation: in real environments, different misconfigurations leak
different identities. Winlogon AutoLogon typically stores a help-desk
account, an Unattend.xml leaks a deployment account, a developer's git
repo leaks a service account, and so on. Mapping each scenario to a
distinct identity from your lab's directory makes the recon trail
realistic and lets each discovered credential unlock a different pivot.

#### New per-scenario credential variables

| Variable | Used by |
|---|---|
| `ludus_win_privesc_winlogon_user` / `_password` | `stored_creds_winlogon.yml` |
| `ludus_win_privesc_ps_history_user` / `_password` | `leaked_creds_ps_history.yml` |
| `ludus_win_privesc_git_user` / `_password` / `_email` / `_dev_name` | `leaked_creds_git.yml` |
| `ludus_win_privesc_files_user` / `_password` | `leaked_creds_files.yml` |
| `ludus_win_privesc_unattend_user` / `_password` | `answer_files.yml` |
| `ludus_win_privesc_office_*` (15 variables) | `office_creds.yml` |

The 15 office variables cover: admin user/password, DC user/password, SQL
password, backup user/password, Veeam user/password, WSUS user/password,
router password, WiFi SSID/password, Linux user/password, break-glass
user/password, and an optional password-manager URL/note.

#### New internal infrastructure variables

These rewrite the previously hardcoded `lab.local` and `10.10.10.x`
references to point at real hosts in your range. They appear in
PS history, git config, .env files, known_hosts, ssh config, IT notes,
and credential CSVs.

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

### Known limitations

- Certain characters in credential values can cause PowerShell or Ansible
  to misbehave when the role embeds those values into `win_shell` tasks.
  See the "Special characters in credential values" section in the README
  for the full list and recommended workarounds. Short version: stick to
  alphanumerics + `! @ # % ^ & * ( ) - _ + = . , ; : ?` for passwords
  used in `_office_*` and `_admin_password`. Avoid `$ ' " \``.

### Changed

- `ssh_private_keys.yml` now writes a `~/.ssh/config` entry for the
  Veeam repo host (`veeamrepo`), in addition to the existing `jumpbox`,
  `dc01`, and `linuxdev` entries.
- The final debug message in `ssh_private_keys.yml` now points at the
  Veeam repo and Linux host using the parameterized values, instead of
  hardcoded `root@10.10.10.30`.

### Documentation

- Added a "Per-Scenario Credential Overrides" section to the README
  explaining when and how to use the new variables.
- Added a "Special characters in credential values" section to the README
  documenting which characters are safe to use in passwords and which
  cause silent corruption (e.g. `$` in a password leads to `Sql!`-style
  truncation when written into the random-folder `deploy.ps1`).
- Added a notice in `hardcoded_creds_dotnet.yml` clarifying that the
  credentials in this scenario are baked into the pre-compiled binary
  and cannot be customized — the discovered creds will not authenticate
  to anything in your range. (Already true upstream; now documented in
  the task file itself.)

### Backward compatibility

Fully preserved. The new variables all default to existing values, so
deployments that don't set any of the new variables behave identically
to the previous version of the role.

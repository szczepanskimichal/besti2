import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getAdminHealth } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDateTime } from "@/lib/format";
import styles from "../admin.module.css";

export const metadata: Metadata = { title: "System" };

// A backup older than this (hours) is shown in red: backup.sh runs every night
const BACKUP_MAX_HOURS = 30;

// How BESTI is doing right now: version, database, last backup, e-mails waiting and the background jobs
export default async function AdminSystemPage() {
    const token = await getToken();
    const health = token ? await getAdminHealth(token) : null;
    if (!health) {
        notFound();
    }
    // eslint-disable-next-line react-hooks/purity -- a server page: "now" once per request
    const now = Date.now();
    const backupOld = !health.lastBackupUtc
        || now - new Date(health.lastBackupUtc).getTime() > BACKUP_MAX_HOURS * 3_600_000;

    return (
        <>
            <h1 className={styles.title}>Systemstatus</h1>
            <section className={styles.tiles} aria-label="Status">
                <div className={styles.tile}>
                    <p className={`${styles.tileValue} ${health.databaseOk ? styles.ok : styles.bad}`}>{health.databaseOk ? "OK" : "Nede"}</p>
                    <p className={styles.tileLabel}>Database{health.databaseSizeMb !== null ? `, ${health.databaseSizeMb.toString().replace(".", ",")} MB av 10 GB` : ""}</p>
                </div>
                <div className={styles.tile}>
                    <p className={`${styles.tileValue} ${backupOld ? styles.bad : styles.ok}`}>
                        {health.lastBackupUtc ? (backupOld ? "Gammel" : "OK") : "Ukjent"}
                    </p>
                    <p className={styles.tileLabel}>
                        Siste backup{health.lastBackupUtc ? `: ${formatDateTime(health.lastBackupUtc)}` : " (se healthchecks.io)"}
                    </p>
                </div>
                <div className={styles.tile}>
                    <p className={`${styles.tileValue} ${health.mailsFailing > 0 ? styles.bad : styles.ok}`}>{health.mailsWaiting}</p>
                    <p className={styles.tileLabel}>E-post i kø ({health.mailsFailing} med feil)</p>
                </div>
                <div className={styles.tile}>
                    <p className={styles.tileValue}>{health.pushDevices}</p>
                    <p className={styles.tileLabel}>Telefoner med push</p>
                </div>
            </section>

            <section className={styles.card}>
                <h2 className={styles.cardTitle}>Versjon</h2>
                <dl className={styles.details}>
                    <dt>Kjører</dt>
                    <dd>{health.version ? health.version.slice(0, 12) : "lokal utvikling"}</dd>
                    <dt>Startet</dt>
                    <dd>{formatDateTime(health.startedUtc)}</dd>
                </dl>
            </section>

            <section className={styles.card}>
                <h2 className={styles.cardTitle}>Bakgrunnsjobber</h2>
                {health.jobs.length === 0 ? <p className={styles.muted}>Ingen har kjørt siden start ennå.</p> : (
                    <ul className={styles.log}>
                        {health.jobs.map((job) => (
                            <li key={job.name}>
                                <span><span className={job.ok ? styles.ok : styles.bad}>{job.ok ? "✓" : "✗"}</span> {job.name}</span>
                                <span className={styles.muted}>Sist {formatDateTime(job.lastRunUtc)}</span>
                            </li>
                        ))}
                    </ul>
                )}
            </section>

            {health.failingMails.length > 0 && (
                <section className={styles.card}>
                    <h2 className={styles.cardTitle}>E-post som feiler</h2>
                    <ul className={styles.log}>
                        {health.failingMails.map((mail) => (
                            <li key={`${mail.createdUtc}-${mail.to}`}>
                                <span>«{mail.subject}» til {mail.to}</span>
                                <span className={styles.muted}>{mail.attempts} forsøk, neste {formatDateTime(mail.nextAttemptUtc)}</span>
                            </li>
                        ))}
                    </ul>
                </section>
            )}
        </>
    );
}

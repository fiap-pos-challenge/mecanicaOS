import { Observable, switchMap, takeWhile, timer } from 'rxjs';
import { isFinished, Job } from './customer-jobs.gateway';

export const JOB_POLL_INTERVAL_MS = 1000;

export function pollJob(
  load: (id: string) => Observable<Job>,
  id: string,
  intervalMs = JOB_POLL_INTERVAL_MS,
): Observable<Job> {
  return timer(intervalMs, intervalMs).pipe(
    switchMap(() => load(id)),
    takeWhile((job) => !isFinished(job), true),
  );
}

export function downloadText(
  documentRef: Document,
  fileName: string,
  content: string,
  type: string,
): void {
  const url = URL.createObjectURL(new Blob([content], { type }));
  const link = documentRef.createElement('a');
  link.href = url;
  link.download = fileName;
  link.rel = 'noopener';
  documentRef.body.append(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

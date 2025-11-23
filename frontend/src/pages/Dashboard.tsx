import { useEffect, useState } from 'react';
import api from '../api/client';
import { DashboardSummaryDto } from '../api/types';

const DashboardPage = () => {
  const [summary, setSummary] = useState<DashboardSummaryDto | null>(null);

  useEffect(() => {
    api.get<DashboardSummaryDto>('/api/dashboard/summary').then((res) => setSummary(res.data));
  }, []);

  return (
    <div className="container">
      <h2>Genel Bakış</h2>
      <div className="grid columns-2">
        <div className="card">
          <strong>Aktif Hesap</strong>
          <div style={{ fontSize: 28 }}>{summary?.activeAccounts ?? '-'}</div>
        </div>
        <div className="card">
          <strong>Günlük Gönderilen</strong>
          <div style={{ fontSize: 28 }}>{summary?.dailySentEmails ?? '-'}</div>
        </div>
        <div className="card">
          <strong>Günlük Reply</strong>
          <div style={{ fontSize: 28 }}>{summary?.dailyReplies ?? '-'}</div>
        </div>
        <div className="card">
          <strong>Bekleyen Job</strong>
          <div style={{ fontSize: 28 }}>{summary?.warmupJobsPending ?? '-'}</div>
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;

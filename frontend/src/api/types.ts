export type AuthResponse = {
  token: string;
  refreshToken: string;
  expiresAt: string;
};

export type MailAccountDto = {
  id: string;
  displayName: string;
  emailAddress: string;
  providerType: number;
  status: number;
  createdAt: string;
  lastHealthCheckAt?: string;
};

export type WarmupProfileDto = {
  id: string;
  mailAccountId: string;
  isEnabled: boolean;
  startDate: string;
  dailyMinEmails: number;
  dailyMaxEmails: number;
  replyRate: number;
  maxDurationDays: number;
  currentDay: number;
  timeWindowStart: string;
  timeWindowEnd: string;
  useRandomization: boolean;
};

export type DashboardSummaryDto = {
  activeAccounts: number;
  dailySentEmails: number;
  dailyReplies: number;
  warmupJobsPending: number;
};

export type ReputationScoreDto = {
  mailAccountId: string;
  emailAddress: string;
  score: number;
  label: string;
  trend: number[];
};

export type DnsCheckDto = {
  mailAccountId: string;
  emailAddress: string;
  spf: string;
  dkim: string;
  dmarc: string;
  mx: string;
  reverseDns: string;
};

export type WarmupLogDto = {
  id: string;
  mailAccountId: string;
  messageId: string;
  direction: number;
  subject: string;
  toAddress: string;
  fromAddress: string;
  sentAt?: string;
  deliveredAt?: string;
  openedAt?: string;
  markedAsImportant: boolean;
  isSpam: boolean;
};

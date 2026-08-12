/**
 * Auth Models
 * Matches backend DTOs for authentication
 */

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  userId: number;
  email: string;
  role: string;
}

export interface ResetPasswordRequest {
  email: string;
  newPassword: string;
}

export interface User {
  userId: number;
  email: string;
  role: string;
}
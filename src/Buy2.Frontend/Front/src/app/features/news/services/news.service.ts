import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import type { CreateNewsPostDto, NewsCategory, NewsPost } from '../models/news.models';
import { effectiveStatus } from '../utils/news.utils';

@Injectable({
  providedIn: 'root',
})
export class NewsService {
  private readonly http = inject(HttpClient);
  private readonly postsUrl = `${environment.jsonServerUrl}/newsPosts`;
  private readonly categoriesUrl = `${environment.jsonServerUrl}/newsCategories`;

  getPosts(): Observable<NewsPost[]> {
    return this.http.get<NewsPost[]>(this.postsUrl).pipe(
      map((posts) => posts.map((post) => ({ ...post, status: effectiveStatus(post) }))),
    );
  }

  getPost(id: string): Observable<NewsPost> {
    return this.http.get<NewsPost>(`${this.postsUrl}/${id}`).pipe(
      map((post) => ({ ...post, status: effectiveStatus(post) })),
    );
  }

  createPost(dto: CreateNewsPostDto): Observable<NewsPost> {
    return this.http.post<NewsPost>(this.postsUrl, dto);
  }

  updatePost(id: string, dto: Partial<CreateNewsPostDto>): Observable<NewsPost> {
    return this.http.patch<NewsPost>(`${this.postsUrl}/${id}`, dto);
  }

  deletePost(id: string): Observable<void> {
    return this.http.delete<void>(`${this.postsUrl}/${id}`);
  }

  getCategories(): Observable<NewsCategory[]> {
    return this.http.get<NewsCategory[]>(this.categoriesUrl);
  }
}

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, switchMap } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import {
  normalizeReactionFields,
  type CreateNewsCommentDto,
  type CreateNewsPostDto,
  type NewsCategory,
  type NewsComment,
  type NewsPost,
} from '../models/news.models';
import { effectiveStatus } from '../utils/news.utils';

@Injectable({
  providedIn: 'root',
})
export class NewsService {
  private readonly http = inject(HttpClient);
  private readonly postsUrl = `${environment.jsonServerUrl}/newsPosts`;
  private readonly categoriesUrl = `${environment.jsonServerUrl}/newsCategories`;
  private readonly commentsUrl = `${environment.jsonServerUrl}/newsComments`;

  getPosts(): Observable<NewsPost[]> {
    return this.http.get<NewsPost[]>(this.postsUrl).pipe(
      map((posts) => posts.map((post) => this.normalizePost(post))),
    );
  }

  getPost(id: string): Observable<NewsPost> {
    return this.http.get<NewsPost>(`${this.postsUrl}/${id}`).pipe(
      map((post) => this.normalizePost(post)),
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

  getComments(postId: string, parentId?: string): Observable<NewsComment[]> {
    return this.http.get<NewsComment[]>(this.commentsUrl).pipe(
      map((comments) =>
        comments
          .map((comment) => this.normalizeComment(comment))
          .filter((comment) => {
            const matchesPost = comment.postId === postId;
            if (parentId === undefined) {
              return matchesPost;
            }
            return matchesPost && comment.parentId === parentId;
          }),
      ),
    );
  }

  createComment(dto: CreateNewsCommentDto): Observable<NewsComment> {
    return this.http.post<NewsComment>(this.commentsUrl, dto).pipe(
      switchMap((created) => this.refreshCommentsCount(created.postId).pipe(map(() => created))),
    );
  }

  updateComment(id: string, dto: Partial<CreateNewsCommentDto>): Observable<NewsComment> {
    return this.http.patch<NewsComment>(`${this.commentsUrl}/${id}`, dto);
  }

  deleteComment(id: string, postId: string): Observable<void> {
    return this.http.delete<void>(`${this.commentsUrl}/${id}`).pipe(
      switchMap(() => this.refreshCommentsCount(postId)),
    );
  }

  private normalizeComment(comment: NewsComment): NewsComment {
    return { ...comment, ...normalizeReactionFields(comment) };
  }

  private normalizePost(post: NewsPost): NewsPost {
    return {
      ...post,
      ...normalizeReactionFields(post),
      status: effectiveStatus(post),
      commentsCount: post.commentsCount ?? 0,
    };
  }

  private refreshCommentsCount(postId: string): Observable<void> {
    return this.getComments(postId).pipe(
      switchMap((comments) => this.updatePost(postId, { commentsCount: comments.length })),
      map(() => undefined),
    );
  }
}
